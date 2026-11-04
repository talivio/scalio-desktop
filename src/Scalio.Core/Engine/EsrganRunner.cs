using Scalio.Core.Diagnostics;

namespace Scalio.Core.Engine;

/// <summary>
/// Tek bir görseli Real-ESRGAN ile büyütür.
///
/// ⚠️ ÇIKIŞ KODUNA GÜVENİLMEZ — bkz. `GpuProbe` üstündeki not. Gerçek exe ile
/// manuel sınamada girdi okunamadığında bile çıkış kodu 0 döndü. Başarı
/// ölçütü burada da ÇIKTI DOSYASININ GERÇEKTEN OLUŞMASIDIR, çıkış kodu değil.
///
/// ⚠️ KAYNAĞA ASLA YAZMAZ: `outputPath`, `inputPath` ile aynıysa çalıştırma
/// baştan reddedilir (Salvio'daki "kaynağa yazma" ilkesiyle aynı ruh — bu
/// sınıf kendi başına hangi dosyanın "kaynak" olduğunu bilmez, o yüzden bu
/// tek güvenlik denetimini burada, en alt katmanda da tekrar eder).
///
/// ⚠️ GÜNLÜKTE DOSYA ADI YAZILMAZ: motorun stderr/stdout satırları kullanıcı
/// dosya adlarını içerebilir (ör. "decode image gizli_belge.jpg failed").
/// Günlüğe yazmadan önce bilinen yollar (`<input>`, `<output>`, `<models>`,
/// `<engine>`) ile değiştirilir.
/// </summary>
public sealed class EsrganRunner
{
    private readonly IProcessRunner _runner;

    public EsrganRunner()
        : this(new ProcessRunner())
    {
    }

    public EsrganRunner(IProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task RunAsync(
        string enginePath,
        string modelsPath,
        string inputPath,
        string outputPath,
        EsrganModel model,
        int scale,
        int? tileSize = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(
                Path.GetFullPath(inputPath),
                Path.GetFullPath(outputPath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Çıktı, kaynak dosyanın üzerine yazamaz.", nameof(outputPath));
        }

        if (!File.Exists(enginePath))
        {
            throw new EngineFailedException("Görsel büyütme motoru bulunamadı.");
        }

        if (!File.Exists(inputPath))
        {
            throw new EngineFailedException("Kaynak görsel bulunamadı.");
        }

        var modelName = model.ToModelName();
        var sanitizer = BuildSanitizer(enginePath, modelsPath, inputPath, outputPath);

        var attemptTile = tileSize;
        var maxAttempts = 2; // ⚠️ Brief: OOM'da tile küçültülüp BİR KEZ daha denenir, sonra vazgeçilir.

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stderrLines = new List<string>();

            var request = new ProcessRunRequest
            {
                FileName = enginePath,
                Arguments = EsrganArguments.Build(inputPath, outputPath, modelName, modelsPath, scale, attemptTile),
            };

            ScalioLog.Info(
                "EsrganRunner",
                $"deneme {attempt}/{maxAttempts} · model={modelName} · ölçek={scale} · tile={(attemptTile?.ToString() ?? "auto")}");

            var result = await _runner.RunAsync(
                request,
                line => ScalioLog.Debug("EsrganRunner", sanitizer(line)),
                line =>
                {
                    stderrLines.Add(line);
                    ScalioLog.Debug("EsrganRunner", sanitizer(line));
                },
                cancellationToken).ConfigureAwait(false);

            if (result.Cancelled)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
            {
                ScalioLog.Info("EsrganRunner", "tamamlandı");

                return;
            }

            var combinedStderr = string.Join('\n', stderrLines);
            var outOfMemory = IsOutOfMemory(combinedStderr);

            if (outOfMemory && attempt < maxAttempts)
            {
                attemptTile = NextSmallerTile(attemptTile);

                ScalioLog.Warn("EsrganRunner", $"bellek yetersiz, tile={attemptTile} ile yeniden deneniyor");

                continue;
            }

            if (outOfMemory)
            {
                throw new EngineOutOfMemoryException(
                    "Motor bellek yetersizliği nedeniyle başarısız oldu (daha küçük tile ile de denendi).");
            }

            var detail = sanitizer(combinedStderr);

            throw new EngineFailedException(
                detail.Length > 0 ? detail : $"Motor başarısız oldu (çıkış kodu {result.ExitCode}).");
        }

        throw new EngineFailedException("Motor başarısız oldu.");
    }

    private static bool IsOutOfMemory(string stderr)
    {
        var lower = stderr.ToLowerInvariant();

        return lower.Contains("out of memory")
               || lower.Contains("outofmemory")
               || lower.Contains("vkallocatememory")
               || lower.Contains("cudamalloc")
               || lower.Contains("failed to allocate");
    }

    /// <summary>400 → 200 → 100 basamaklarını izleyen bir sonraki küçük tile.</summary>
    private static int NextSmallerTile(int? current)
    {
        if (current is null)
        {
            return 400;
        }

        if (current > 200)
        {
            return 200;
        }

        if (current > 100)
        {
            return 100;
        }

        return Math.Max(32, current.Value / 2);
    }

    private static Func<string, string> BuildSanitizer(string enginePath, string modelsPath, string inputPath, string outputPath)
    {
        return line => line
            .Replace(inputPath, "<input>", StringComparison.OrdinalIgnoreCase)
            .Replace(outputPath, "<output>", StringComparison.OrdinalIgnoreCase)
            .Replace(modelsPath, "<models>", StringComparison.OrdinalIgnoreCase)
            .Replace(enginePath, "<engine>", StringComparison.OrdinalIgnoreCase);
    }
}

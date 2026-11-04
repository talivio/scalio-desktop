using ImageMagick;

namespace Scalio.Core.Engine;

/// <summary>Bu makinede motorun çalışıp çalışmayacağı.</summary>
public enum GpuAvailability
{
    Available,
    Unavailable,
}

/// <summary>Bir GPU sınamasının sonucu.</summary>
public sealed class GpuProbeResult
{
    public required GpuAvailability Availability { get; init; }

    /// <summary>Kullanıcıya gösterilebilecek kısa açıklama (Unavailable ise dolu).</summary>
    public string? Message { get; init; }
}

/// <summary>
/// Bu bilgisayarda Real-ESRGAN motorunun çalışıp çalışmadığını kontrol eder.
///
/// ⚠️ NEDEN `-h` KULLANILMIYOR: gerçek exe ile manuel sınamada (bkz. görev
/// raporu) `-h` hiçbir Vulkan/GPU numaralandırması YAPMIYOR — yalnızca kullanım
/// metnini yazıp çıkıyor (çıkış kodu bile tutarsız: -h için -1, geçersiz
/// model için 0xC0000409 gibi platforma özgü değerler gördük). GPU'nun
/// gerçekten kullanılabilir olup olmadığı yalnızca GERÇEK bir çalıştırmada
/// (ilk stderr satırlarında `[0 <cihaz adı>]` biçiminde cihaz listesi) ortaya
/// çıkıyor. Bu yüzden `GpuProbe` küçük (8x8) bir sınama görseli üretip motoru
/// GERÇEKTEN bir kez çalıştırır ve çıktı dosyasının oluşup oluşmadığına bakar.
///
/// ⚠️ ÇIKIŞ KODUNA GÜVENİLMEZ: aynı manuel sınamada, girdi dosyası
/// okunamadığında bile motor çıkış kodu 0 ile döndü ("decode image ... failed"
/// stderr'e yazıldı, çıktı dosyası hiç oluşmadı). Bu yüzden hem burada hem
/// `EsrganRunner`'da başarı ölçütü ÇIKIŞ KODU DEĞİL, çıktı dosyasının
/// gerçekten oluşup oluşmadığıdır.
/// </summary>
public sealed class GpuProbe
{
    private readonly IProcessRunner _runner;

    public GpuProbe()
        : this(new ProcessRunner())
    {
    }

    public GpuProbe(IProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<GpuProbeResult> ProbeAsync(string enginePath, string modelsPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(enginePath))
        {
            return Unavailable("Görsel büyütme motoru bulunamadı.");
        }

        if (!Directory.Exists(modelsPath))
        {
            return Unavailable("Motor model dosyaları bulunamadı.");
        }

        var probeDirectory = Path.Combine(Path.GetTempPath(), "Scalio", "gpuprobe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(probeDirectory);

        var inputPath = Path.Combine(probeDirectory, "probe.png");
        var outputPath = Path.Combine(probeDirectory, "probe-out.png");

        try
        {
            using (var image = new MagickImage(MagickColors.White, 8, 8))
            {
                image.Write(inputPath);
            }

            var stderrLines = new List<string>();

            var request = new ProcessRunRequest
            {
                FileName = enginePath,
                Arguments = EsrganArguments.Build(inputPath, outputPath, "realesrgan-x4plus", modelsPath, scale: 2, tileSize: 32),
            };

            await _runner.RunAsync(request, _ => { }, line => stderrLines.Add(line), cancellationToken)
                .ConfigureAwait(false);

            if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
            {
                return new GpuProbeResult { Availability = GpuAvailability.Available };
            }

            return Unavailable(DescribeFailure(stderrLines));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            return Unavailable("Görsel büyütme motoru başlatılamadı.");
        }
        finally
        {
            try
            {
                Directory.Delete(probeDirectory, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Sınama klasörü silinemedi — kritik değil, temp altında kalır.
            }
        }
    }

    private static GpuProbeResult Unavailable(string message) =>
        new() { Availability = GpuAvailability.Unavailable, Message = message };

    private static string DescribeFailure(List<string> stderrLines)
    {
        var combined = string.Join(' ', stderrLines).ToLowerInvariant();

        if (combined.Contains("vulkan") || combined.Contains("gpu") || combined.Contains("device"))
        {
            return "Bu bilgisayarda görsel büyütme çalışmıyor (uyumlu grafik kartı/Vulkan bulunamadı).";
        }

        return "Bu bilgisayarda görsel büyütme çalışmıyor.";
    }
}

using ImageMagick;
using Scalio.Core.Diagnostics;
using Scalio.Core.Engine;

namespace Scalio.Core.Pipeline;

/// <summary>Bir işin tamamlanma sonucu.</summary>
public sealed record UpscaleResult(string OutputPath);

/// <summary>
/// `UpscaleJob`'ı işleyen boru hattı: motorla büyüt → (istenirse) hedef
/// genişliğe küçült → (istenirse) merkezden kırp → biçime/kaliteye dönüştür.
///
/// ⚠️ KAYNAĞA ASLA YAZMAZ, VAR OLAN ÇIKTIYA DA YAZMAZ. Motor çıktısı önce
/// AYRI bir geçici dosyaya yazılır (Salvio'nun "kaynağa yazma" ilkesiyle aynı
/// ruh — bkz. `EsrganRunner`); son çıktı adı klasörde çakışıyorsa `-1`, `-2`
/// eki eklenerek üretilir, hiçbir zaman üzerine yazılmaz.
/// </summary>
public sealed class UpscalePipeline
{
    private readonly IUpscaleEngine _engine;

    public UpscalePipeline(IUpscaleEngine engine)
    {
        _engine = engine;
    }

    public async Task<UpscaleResult> RunAsync(UpscaleJob job, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(job.SourcePath))
        {
            throw new FileNotFoundException("Kaynak görsel bulunamadı.", job.SourcePath);
        }

        Directory.CreateDirectory(job.OutputFolder);

        /*
         * ⚠️ MOTOR HER ZAMAN x4 ÇALIŞIR. Paketteki modeller (realesrgan-x4plus,
         * -x4plus-anime) yalnız x4 eğitildi; `-s 2`/`-s 3` verildiğinde motor
         * çıkış kodu 0 ile ve doğru ÖLÇÜDE ama BOZUK bir görsel üretiyor
         * (yalnız sol üst bölge, kenarlarda şerit). Gerçek exe ile görüldü.
         * x2/x3, x4 sonucunun Lanczos ile küçültülmesiyle elde edilir.
         */
        const int engineScale = 4;

        var info = new MagickImageInfo(job.SourcePath);
        var tileSize = TileSizer.Suggest((int)info.Width, (int)info.Height);

        var tempDirectory = Path.Combine(Path.GetTempPath(), "Scalio", "jobs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var enginedOutput = Path.Combine(tempDirectory, "engine-output.png");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _engine.UpscaleAsync(job.SourcePath, enginedOutput, job.Model, engineScale, tileSize, cancellationToken)
                .ConfigureAwait(false);

            using var image = new MagickImage(enginedOutput);

            if (job.Scale is { } scale && scale != engineScale)
            {
                image.FilterType = FilterType.Lanczos;
                image.Resize(new MagickGeometry((uint)(info.Width * scale), (uint)(info.Height * scale)) { IgnoreAspectRatio = true });
            }

            if (job.TargetWidth is { } targetWidth)
            {
                var targetHeight = (uint)Math.Max(1, Math.Round(image.Height * (targetWidth / (double)image.Width)));

                image.FilterType = FilterType.Lanczos;
                image.Resize(new MagickGeometry((uint)targetWidth, targetHeight) { IgnoreAspectRatio = false });
            }

            if (job.Crop is { } preset)
            {
                CropCenterToAspect(image, preset);
            }

            ApplyFormat(image, job.Format, job.Quality);

            var extension = ExtensionFor(job.Format);
            var baseName = Path.GetFileNameWithoutExtension(job.SourcePath);
            var outputPath = ResolveNonCollidingPath(job.OutputFolder, baseName, extension);

            image.Write(outputPath);

            ScalioLog.Info("UpscalePipeline", "iş tamamlandı");

            return new UpscaleResult(outputPath);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Geçici klasör silinemedi — kritik değil.
            }
        }
    }

    /// <summary>Görseli, verilen ön ayarın en-boy oranına göre merkezden kırpıp tam ölçüye getirir.</summary>
    private static void CropCenterToAspect(MagickImage image, CropPreset preset)
    {
        var currentAspect = (double)image.Width / image.Height;

        uint cropWidth, cropHeight;

        if (currentAspect > preset.AspectRatio)
        {
            // Görsel hedeften daha geniş: yanlardan kırp.
            cropHeight = image.Height;
            cropWidth = (uint)Math.Round(image.Height * preset.AspectRatio);
        }
        else
        {
            // Görsel hedeften daha uzun (dar): üst/alttan kırp.
            cropWidth = image.Width;
            cropHeight = (uint)Math.Round(image.Width / preset.AspectRatio);
        }

        var x = (int)((image.Width - cropWidth) / 2);
        var y = (int)((image.Height - cropHeight) / 2);

        image.Crop(new MagickGeometry(x, y, cropWidth, cropHeight));
        image.ResetPage();

        image.FilterType = FilterType.Lanczos;
        image.Resize(new MagickGeometry((uint)preset.Width, (uint)preset.Height) { IgnoreAspectRatio = true });
    }

    private static void ApplyFormat(MagickImage image, OutputFormat format, int quality)
    {
        switch (format)
        {
            case OutputFormat.Png:
                image.Format = MagickFormat.Png;
                break;
            case OutputFormat.Jpeg:
                image.Format = MagickFormat.Jpg;
                image.Quality = (uint)quality;
                break;
            case OutputFormat.WebP:
                image.Format = MagickFormat.WebP;
                image.Quality = (uint)quality;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private static string ExtensionFor(OutputFormat format) => format switch
    {
        OutputFormat.Png => ".png",
        OutputFormat.Jpeg => ".jpg",
        OutputFormat.WebP => ".webp",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };

    /// <summary>
    /// Klasörde `baseName.ext` doluysa `baseName-1.ext`, `baseName-2.ext`... dener.
    ///
    /// ⚠️ Var olan hiçbir dosyanın üzerine yazılmaz — Salvio'daki ilke.
    /// </summary>
    internal static string ResolveNonCollidingPath(string folder, string baseName, string extension)
    {
        var candidate = Path.Combine(folder, baseName + extension);

        if (!File.Exists(candidate))
        {
            return candidate;
        }

        for (var suffix = 1; ; suffix++)
        {
            candidate = Path.Combine(folder, $"{baseName}-{suffix}{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}

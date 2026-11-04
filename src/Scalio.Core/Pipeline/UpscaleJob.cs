using Scalio.Core.Engine;

namespace Scalio.Core.Pipeline;

/// <summary>Çıktı dosya biçimi.</summary>
public enum OutputFormat
{
    Png,
    Jpeg,
    WebP,
}

/// <summary>
/// Bir büyütme işinin tanımı.
///
/// ⚠️ `Scale` VE `TargetWidth` BİRLİKTE OLAMAZ — brief'in kararı: ya sabit bir
/// motor ölçeği (x2/x3/x4) istenir, ya da bir hedef genişlik (motor x4 çalışır,
/// ardından Lanczos ile küçültülür). İkisi de verilirse hangisinin öncelikli
/// olacağı belirsiz kalacağından `Create` bunu reddeder.
/// </summary>
public sealed class UpscaleJob
{
    private UpscaleJob()
    {
    }

    public required string SourcePath { get; init; }

    /// <summary>x2/x3/x4 — motor doğrudan bu ölçekte çalışır. `TargetWidth` ile birlikte olamaz.</summary>
    public int? Scale { get; init; }

    /// <summary>Hedef genişlik (px) — motor x4 çalışır, sonra Lanczos ile küçültülür.</summary>
    public int? TargetWidth { get; init; }

    public required EsrganModel Model { get; init; }

    public required OutputFormat Format { get; init; }

    /// <summary>JPEG/WebP kalitesi (1-100). PNG için göz ardı edilir.</summary>
    public int Quality { get; init; } = 90;

    /// <summary>Verilirse büyütme sonrası bu orana göre merkezden kırpılır.</summary>
    public CropPreset? Crop { get; init; }

    public required string OutputFolder { get; init; }

    public static UpscaleJob Create(
        string sourcePath,
        EsrganModel model,
        OutputFormat format,
        string outputFolder,
        int? scale = null,
        int? targetWidth = null,
        int quality = 90,
        CropPreset? crop = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Kaynak yol boş olamaz.", nameof(sourcePath));
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            throw new ArgumentException("Çıktı klasörü boş olamaz.", nameof(outputFolder));
        }

        if (scale is not null && targetWidth is not null)
        {
            throw new ArgumentException("`Scale` ve `TargetWidth` aynı anda verilemez.");
        }

        if (scale is null && targetWidth is null)
        {
            throw new ArgumentException("`Scale` ya da `TargetWidth` verilmelidir.");
        }

        if (scale is not (null or 2 or 3 or 4))
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Ölçek yalnızca 2, 3 ya da 4 olabilir.");
        }

        if (targetWidth is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetWidth), targetWidth, "Hedef genişlik pozitif olmalı.");
        }

        if (quality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), quality, "Kalite 1-100 arasında olmalı.");
        }

        return new UpscaleJob
        {
            SourcePath = sourcePath,
            Scale = scale,
            TargetWidth = targetWidth,
            Model = model,
            Format = format,
            Quality = quality,
            Crop = crop,
            OutputFolder = outputFolder,
        };
    }
}

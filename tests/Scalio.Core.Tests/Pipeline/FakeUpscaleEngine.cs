using ImageMagick;
using Scalio.Core.Engine;
using Scalio.Core.Pipeline;

namespace Scalio.Core.Tests.Pipeline;

/// <summary>
/// `IUpscaleEngine` sahtesi — gerçek motor/GPU olmadan boru hattını sınamak
/// için, kaynağı Magick.NET'le basitçe büyütüp verilen çıktı yoluna yazar.
/// </summary>
public sealed class FakeUpscaleEngine : IUpscaleEngine
{
    public int CallCount { get; private set; }

    public List<int> ScalesUsed { get; } = new();

    public List<int?> TileSizesUsed { get; } = new();

    public Func<string, string, int, CancellationToken, Task>? Behavior { get; set; }

    public async Task UpscaleAsync(
        string inputPath,
        string outputPath,
        EsrganModel model,
        int scale,
        int? tileSize,
        CancellationToken cancellationToken)
    {
        CallCount++;
        ScalesUsed.Add(scale);
        TileSizesUsed.Add(tileSize);

        if (Behavior is not null)
        {
            await Behavior(inputPath, outputPath, scale, cancellationToken);

            return;
        }

        using var image = new MagickImage(inputPath);

        image.FilterType = FilterType.Lanczos;
        image.Resize(new Percentage(scale * 100));
        image.Write(outputPath);
    }
}

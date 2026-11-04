namespace Scalio.Core.Pipeline;

/// <summary>Hazır kırpma ölçüsü — hedef genişlik/yükseklik, oran bundan türer.</summary>
public sealed record CropPreset(string Name, int Width, int Height)
{
    public double AspectRatio => (double)Width / Height;
}

/// <summary>Sabit hazır kırpma ön ayarları listesi.</summary>
public static class CropPresets
{
    public static readonly CropPreset Hero = new("Hero", 2560, 1440);

    public static readonly CropPreset Card = new("Card", 1200, 800);

    public static readonly CropPreset InstagramPortrait = new("InstagramPortrait", 1080, 1350);

    public static readonly CropPreset OpenGraph = new("OpenGraph", 1200, 630);

    public static IReadOnlyList<CropPreset> All { get; } = new[]
    {
        Hero,
        Card,
        InstagramPortrait,
        OpenGraph,
    };
}

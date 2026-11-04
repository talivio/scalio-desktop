using ImageMagick;
using Scalio.Core.Engine;
using Scalio.Core.Pipeline;

namespace Scalio.Core.Tests.Pipeline;

public sealed class UpscalePipelineTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "ScalioTests", Guid.NewGuid().ToString("N"));
    private readonly string _sourcePath;
    private readonly string _outputFolder;

    public UpscalePipelineTests()
    {
        Directory.CreateDirectory(_tempDirectory);

        _outputFolder = Path.Combine(_tempDirectory, "out");
        Directory.CreateDirectory(_outputFolder);

        _sourcePath = Path.Combine(_tempDirectory, "photo.png");

        using var image = new MagickImage(MagickColors.SkyBlue, 100, 80);
        image.Write(_sourcePath);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task Scale_job_produces_output_with_expected_dimensions()
    {
        var engine = new FakeUpscaleEngine();
        var pipeline = new UpscalePipeline(engine);

        var job = UpscaleJob.Create(_sourcePath, EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2);

        var result = await pipeline.RunAsync(job);

        Assert.True(File.Exists(result.OutputPath));

        using var output = new MagickImage(result.OutputPath);
        Assert.Equal(200u, output.Width);
        Assert.Equal(160u, output.Height);

        // Modeller yalnız x4 destekliyor; motora `-s 2` gitmesi bozuk çıktı üretiyordu.
        Assert.Equal(4, engine.ScalesUsed.Single());
    }

    [Fact]
    public async Task TargetWidth_job_runs_engine_at_scale_4_then_resizes_with_lanczos()
    {
        var engine = new FakeUpscaleEngine();
        var pipeline = new UpscalePipeline(engine);

        var job = UpscaleJob.Create(_sourcePath, EsrganModel.Photo, OutputFormat.Png, _outputFolder, targetWidth: 150);

        var result = await pipeline.RunAsync(job);

        Assert.Equal(4, engine.ScalesUsed.Single());

        using var output = new MagickImage(result.OutputPath);
        Assert.Equal(150u, output.Width);
        Assert.Equal(120u, output.Height); // 100x80 kaynağın oranı korunur (80/100*150=120)
    }

    [Fact]
    public async Task Crop_preset_produces_exact_target_dimensions()
    {
        var engine = new FakeUpscaleEngine();
        var pipeline = new UpscalePipeline(engine);

        var job = UpscaleJob.Create(
            _sourcePath, EsrganModel.Photo, OutputFormat.Png, _outputFolder,
            scale: 4, crop: CropPresets.Card);

        var result = await pipeline.RunAsync(job);

        using var output = new MagickImage(result.OutputPath);
        Assert.Equal((uint)CropPresets.Card.Width, output.Width);
        Assert.Equal((uint)CropPresets.Card.Height, output.Height);
    }

    [Fact]
    public async Task Jpeg_format_writes_jpg_extension_with_requested_quality()
    {
        var engine = new FakeUpscaleEngine();
        var pipeline = new UpscalePipeline(engine);

        var job = UpscaleJob.Create(
            _sourcePath, EsrganModel.Photo, OutputFormat.Jpeg, _outputFolder,
            scale: 2, quality: 55);

        var result = await pipeline.RunAsync(job);

        Assert.EndsWith(".jpg", result.OutputPath);

        using var output = new MagickImage(result.OutputPath);
        Assert.Equal(MagickFormat.Jpeg, output.Format);
    }

    [Fact]
    public async Task Never_overwrites_existing_output_and_source_stays_untouched()
    {
        var engine = new FakeUpscaleEngine();
        var pipeline = new UpscalePipeline(engine);

        var job = UpscaleJob.Create(_sourcePath, EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2);

        var expectedFirstPath = Path.Combine(_outputFolder, "photo.png");
        File.WriteAllText(expectedFirstPath, "önceden var olan dosya, dokunulmamalı");

        var sourceBytesBefore = File.ReadAllBytes(_sourcePath);

        var result = await pipeline.RunAsync(job);

        Assert.Equal(Path.Combine(_outputFolder, "photo-1.png"), result.OutputPath);
        Assert.Equal("önceden var olan dosya, dokunulmamalı", File.ReadAllText(expectedFirstPath));

        Assert.Equal(sourceBytesBefore, File.ReadAllBytes(_sourcePath));
    }

    [Fact]
    public void ResolveNonCollidingPath_skips_all_taken_names()
    {
        Directory.CreateDirectory(_outputFolder);
        File.WriteAllText(Path.Combine(_outputFolder, "a.png"), "1");
        File.WriteAllText(Path.Combine(_outputFolder, "a-1.png"), "2");

        var resolved = UpscalePipeline.ResolveNonCollidingPath(_outputFolder, "a", ".png");

        Assert.Equal(Path.Combine(_outputFolder, "a-2.png"), resolved);
    }
}

using Scalio.Core.Engine;

namespace Scalio.Core.Tests.Engine;

public sealed class EsrganRunnerTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "ScalioTests", Guid.NewGuid().ToString("N"));
    private readonly string _enginePath;
    private readonly string _modelsPath;
    private readonly string _inputPath;

    public EsrganRunnerTests()
    {
        Directory.CreateDirectory(_tempDirectory);

        _enginePath = Path.Combine(_tempDirectory, "realesrgan-ncnn-vulkan.exe");
        File.WriteAllText(_enginePath, "sahte exe");

        _modelsPath = Path.Combine(_tempDirectory, "models");
        Directory.CreateDirectory(_modelsPath);

        _inputPath = Path.Combine(_tempDirectory, "input.png");
        File.WriteAllBytes(_inputPath, new byte[] { 1, 2, 3 });
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
    public async Task Throws_when_engine_missing()
    {
        var runner = new EsrganRunner(new FakeProcessRunner());

        var missingEnginePath = Path.Combine(_tempDirectory, "yok.exe");
        var outputPath = Path.Combine(_tempDirectory, "out.png");

        await Assert.ThrowsAsync<EngineFailedException>(() =>
            runner.RunAsync(missingEnginePath, _modelsPath, _inputPath, outputPath, EsrganModel.Photo, scale: 4));
    }

    [Fact]
    public async Task Throws_when_input_missing()
    {
        var runner = new EsrganRunner(new FakeProcessRunner());

        var missingInputPath = Path.Combine(_tempDirectory, "yok.png");
        var outputPath = Path.Combine(_tempDirectory, "out.png");

        await Assert.ThrowsAsync<EngineFailedException>(() =>
            runner.RunAsync(_enginePath, _modelsPath, missingInputPath, outputPath, EsrganModel.Photo, scale: 4));
    }

    [Fact]
    public async Task Throws_when_output_equals_input()
    {
        var runner = new EsrganRunner(new FakeProcessRunner());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            runner.RunAsync(_enginePath, _modelsPath, _inputPath, _inputPath, EsrganModel.Photo, scale: 4));
    }

    [Fact]
    public async Task Succeeds_when_output_file_is_created()
    {
        var outputPath = Path.Combine(_tempDirectory, "out.png");

        var fake = new FakeProcessRunner((request, onStdErr) =>
        {
            File.WriteAllBytes(outputPath, new byte[] { 9, 9 });

            return new ProcessRunResult { ExitCode = 0, Cancelled = false };
        });

        var runner = new EsrganRunner(fake);

        await runner.RunAsync(_enginePath, _modelsPath, _inputPath, outputPath, EsrganModel.Photo, scale: 4);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task Exit_code_zero_without_output_file_is_treated_as_failure()
    {
        // ⚠️ Gerçek exe ile gözlemlenen davranış: girdi okunamadığında bile
        // çıkış kodu 0 dönüyor. Başarı ölçütü çıktı dosyasının varlığıdır.
        var outputPath = Path.Combine(_tempDirectory, "out.png");

        var fake = new FakeProcessRunner((request, onStdErr) =>
        {
            onStdErr("decode image input.png failed");

            return new ProcessRunResult { ExitCode = 0, Cancelled = false };
        });

        var runner = new EsrganRunner(fake);

        var exception = await Assert.ThrowsAsync<EngineFailedException>(() =>
            runner.RunAsync(_enginePath, _modelsPath, _inputPath, outputPath, EsrganModel.Photo, scale: 4));

        Assert.Contains("decode image", exception.Message);
    }

    [Fact]
    public async Task Does_not_log_full_file_paths_on_failure()
    {
        var outputPath = Path.Combine(_tempDirectory, "out.png");

        var fake = new FakeProcessRunner((request, onStdErr) =>
        {
            onStdErr($"decode image {_inputPath} failed");

            return new ProcessRunResult { ExitCode = 0, Cancelled = false };
        });

        var runner = new EsrganRunner(fake);

        var exception = await Assert.ThrowsAsync<EngineFailedException>(() =>
            runner.RunAsync(_enginePath, _modelsPath, _inputPath, outputPath, EsrganModel.Photo, scale: 4));

        Assert.DoesNotContain(_inputPath, exception.Message);
        Assert.Contains("<input>", exception.Message);
    }

    [Fact]
    public async Task Retries_once_with_smaller_tile_on_out_of_memory_then_succeeds()
    {
        var outputPath = Path.Combine(_tempDirectory, "out.png");

        var fake = new FakeProcessRunner(
            (request, onStdErr) =>
            {
                onStdErr("cudaMalloc failed: out of memory");

                return new ProcessRunResult { ExitCode = 1, Cancelled = false };
            },
            (request, onStdErr) =>
            {
                File.WriteAllBytes(outputPath, new byte[] { 1 });

                return new ProcessRunResult { ExitCode = 0, Cancelled = false };
            });

        var runner = new EsrganRunner(fake);

        await runner.RunAsync(_enginePath, _modelsPath, _inputPath, outputPath, EsrganModel.Photo, scale: 4, tileSize: null);

        Assert.True(File.Exists(outputPath));
        Assert.Equal(2, fake.ArgumentsLog.Count);
        Assert.DoesNotContain("-t", fake.ArgumentsLog[0]);
        Assert.Contains("-t 400", fake.ArgumentsLog[1]);
    }

    [Fact]
    public async Task Throws_out_of_memory_after_single_retry_fails_too()
    {
        var outputPath = Path.Combine(_tempDirectory, "out.png");

        var fake = new FakeProcessRunner(
            (request, onStdErr) =>
            {
                onStdErr("out of memory");

                return new ProcessRunResult { ExitCode = 1, Cancelled = false };
            },
            (request, onStdErr) =>
            {
                onStdErr("out of memory");

                return new ProcessRunResult { ExitCode = 1, Cancelled = false };
            });

        var runner = new EsrganRunner(fake);

        await Assert.ThrowsAsync<EngineOutOfMemoryException>(() =>
            runner.RunAsync(_enginePath, _modelsPath, _inputPath, outputPath, EsrganModel.Photo, scale: 4));

        Assert.Equal(2, fake.ArgumentsLog.Count);
    }

    [Fact]
    public async Task Throws_operation_cancelled_when_process_was_killed()
    {
        var outputPath = Path.Combine(_tempDirectory, "out.png");

        var fake = new FakeProcessRunner((request, onStdErr) =>
            new ProcessRunResult { ExitCode = -1, Cancelled = true });

        var runner = new EsrganRunner(fake);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            runner.RunAsync(_enginePath, _modelsPath, _inputPath, outputPath, EsrganModel.Photo, scale: 4));
    }
}

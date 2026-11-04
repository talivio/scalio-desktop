using Scalio.Core.Engine;

namespace Scalio.Core.Pipeline;

/// <summary>
/// `UpscalePipeline`'ın motor adımı için gördüğü soyutlama.
///
/// ⚠️ NEDEN VAR: `UpscalePipeline`'ın resize/kırpma/format mantığını gerçek
/// motor (GPU/exe) olmadan test edebilmek için. Üretimde `EsrganUpscaleEngine`
/// kullanılır; testler bunun yerine sahte, hızlı bir uygulama enjekte eder.
/// </summary>
public interface IUpscaleEngine
{
    Task UpscaleAsync(
        string inputPath,
        string outputPath,
        EsrganModel model,
        int scale,
        int? tileSize,
        CancellationToken cancellationToken);
}

/// <summary>`IUpscaleEngine`'in gerçek uygulaması — `EsrganRunner`'ı sabit bir motor/model yoluna bağlar.</summary>
public sealed class EsrganUpscaleEngine : IUpscaleEngine
{
    private readonly string _enginePath;
    private readonly string _modelsPath;
    private readonly EsrganRunner _runner;

    public EsrganUpscaleEngine(string enginePath, string modelsPath, EsrganRunner? runner = null)
    {
        _enginePath = enginePath;
        _modelsPath = modelsPath;
        _runner = runner ?? new EsrganRunner();
    }

    public Task UpscaleAsync(
        string inputPath,
        string outputPath,
        EsrganModel model,
        int scale,
        int? tileSize,
        CancellationToken cancellationToken) =>
        _runner.RunAsync(_enginePath, _modelsPath, inputPath, outputPath, model, scale, tileSize, cancellationToken);
}

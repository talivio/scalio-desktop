namespace Scalio.Core.Engine;

/// <summary>Tek bir alt süreç çalıştırma isteği.</summary>
public sealed class ProcessRunRequest
{
    public required string FileName { get; init; }

    public required string Arguments { get; init; }

    public string? WorkingDirectory { get; init; }
}

/// <summary>Alt sürecin tamamlanma sonucu.</summary>
public sealed class ProcessRunResult
{
    public required int ExitCode { get; init; }

    public required bool Cancelled { get; init; }
}

/// <summary>
/// Alt süreç başlatma soyutlaması.
///
/// ⚠️ NEDEN VAR: `EsrganRunner`'ı gerçek `realesrgan-ncnn-vulkan.exe` olmadan
/// (CI'da, geliştirme makinesinde GPU/exe yokken) test edebilmek için. Gerçek
/// süreç başlatan tek uygulama <see cref="ProcessRunner"/>'dır; testler sahte
/// bir uygulama enjekte eder.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Süreci başlatır, stdout/stderr satırlarını akıtır ve çıkışı bekler.
    /// </summary>
    /// <param name="request">Çalıştırılacak komut.</param>
    /// <param name="onStdOut">Her stdout satırı için çağrılır.</param>
    /// <param name="onStdErr">Her stderr satırı için çağrılır.</param>
    /// <param name="cancellationToken">İptal edilirse süreç `Kill()` edilir.</param>
    Task<ProcessRunResult> RunAsync(
        ProcessRunRequest request,
        Action<string> onStdOut,
        Action<string> onStdErr,
        CancellationToken cancellationToken);
}

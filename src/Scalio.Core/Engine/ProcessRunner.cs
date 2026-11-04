using System.Diagnostics;

namespace Scalio.Core.Engine;

/// <summary>
/// `IProcessRunner`'ın gerçek uygulaması — `System.Diagnostics.Process` sarmalayıcısı.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        ProcessRunRequest request,
        Action<string> onStdOut,
        Action<string> onStdErr,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            Arguments = request.Arguments,
            WorkingDirectory = request.WorkingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onStdOut(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onStdErr(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var cancelled = false;

        await using (cancellationToken.Register(() =>
        {
            cancelled = true;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Süreç zaten çıkmış — yarış durumu, güvenle görmezden gelinir.
            }
        }))
        {
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return new ProcessRunResult { ExitCode = cancelled ? -1 : process.ExitCode, Cancelled = cancelled };
    }
}

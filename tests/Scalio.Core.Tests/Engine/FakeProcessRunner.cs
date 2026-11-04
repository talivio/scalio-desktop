using Scalio.Core.Engine;

namespace Scalio.Core.Tests.Engine;

/// <summary>Sırayla tüketilen, önceden tanımlı davranışlarla `IProcessRunner` sahtesi.</summary>
public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Queue<Func<ProcessRunRequest, Action<string>, ProcessRunResult>> _responses;

    public FakeProcessRunner(params Func<ProcessRunRequest, Action<string>, ProcessRunResult>[] responses)
    {
        _responses = new Queue<Func<ProcessRunRequest, Action<string>, ProcessRunResult>>(responses);
    }

    public List<string> ArgumentsLog { get; } = new();

    public Task<ProcessRunResult> RunAsync(
        ProcessRunRequest request,
        Action<string> onStdOut,
        Action<string> onStdErr,
        CancellationToken cancellationToken)
    {
        ArgumentsLog.Add(request.Arguments);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("FakeProcessRunner: beklenmeyen ek çağrı.");
        }

        var response = _responses.Dequeue();

        return Task.FromResult(response(request, onStdErr));
    }
}

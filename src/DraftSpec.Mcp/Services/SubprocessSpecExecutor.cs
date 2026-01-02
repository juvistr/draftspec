using DraftSpec.Mcp.Models;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Executes specs via subprocess with optional progress callback.
/// Adapts ISpecExecutionService to the ISpecExecutor interface.
/// </summary>
public class SubprocessSpecExecutor : ISpecExecutor
{
    private readonly ISpecExecutionService _executionService;
    private readonly Func<SpecProgressNotification, Task>? _onProgress;
    private readonly CancellationToken _cancellationToken;

    public SubprocessSpecExecutor(
        ISpecExecutionService executionService,
        Func<SpecProgressNotification, Task>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        _executionService = executionService;
        _onProgress = onProgress;
        _cancellationToken = cancellationToken;
    }

    public async Task<RunSpecResult> ExecuteAsync(
        string content,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        // Use the passed cancellation token, falling back to the one from constructor
        var effectiveCt = ct != default ? ct : _cancellationToken;

        if (_onProgress != null)
        {
            return await _executionService.ExecuteSpecAsync(
                content,
                timeout,
                _onProgress,
                effectiveCt).ConfigureAwait(false);
        }

        return await _executionService.ExecuteSpecAsync(
            content,
            timeout,
            effectiveCt).ConfigureAwait(false);
    }
}

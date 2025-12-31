using System.Collections.Concurrent;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Rate limiter for spec execution with concurrency and per-minute limits.
/// </summary>
public sealed class ExecutionRateLimiter : IDisposable
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly ConcurrentQueue<DateTime> _executionTimestamps;
    private readonly int _maxPerMinute;
    private readonly object _cleanupLock = new();
    private bool _disposed;

    public ExecutionRateLimiter(McpOptions options)
    {
        _concurrencySemaphore = new SemaphoreSlim(options.MaxConcurrentExecutions);
        _executionTimestamps = new ConcurrentQueue<DateTime>();
        _maxPerMinute = options.MaxExecutionsPerMinute;
    }

    /// <summary>
    /// Number of currently running executions.
    /// </summary>
    public int CurrentConcurrentExecutions =>
        _concurrencySemaphore.CurrentCount == 0 ? 0 :
        ((SemaphoreSlim)_concurrencySemaphore).CurrentCount;

    /// <summary>
    /// Attempts to acquire a rate limit slot. Returns false if limits are exceeded.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if slot acquired, false if rate limited</returns>
    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        // Check per-minute limit first (quick check without blocking)
        if (!CheckPerMinuteLimit())
            return false;

        // Try to acquire concurrency semaphore (non-blocking check)
        if (!await _concurrencySemaphore.WaitAsync(0, cancellationToken))
            return false;

        // Record this execution timestamp
        _executionTimestamps.Enqueue(DateTime.UtcNow);

        return true;
    }

    /// <summary>
    /// Releases a rate limit slot after execution completes.
    /// </summary>
    public void Release()
    {
        _concurrencySemaphore.Release();
    }

    private bool CheckPerMinuteLimit()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-1);

        // Clean up old timestamps
        lock (_cleanupLock)
        {
            while (_executionTimestamps.TryPeek(out var oldest) && oldest < cutoff)
            {
                _executionTimestamps.TryDequeue(out _);
            }
        }

        // Check if we're under the limit
        return _executionTimestamps.Count < _maxPerMinute;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _concurrencySemaphore.Dispose();
    }
}

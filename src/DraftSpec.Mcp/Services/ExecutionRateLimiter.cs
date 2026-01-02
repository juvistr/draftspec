using System.Collections.Concurrent;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Rate limiter for spec execution with concurrency and per-minute limits.
/// </summary>
public sealed class ExecutionRateLimiter : IDisposable
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly ConcurrentQueue<DateTimeOffset> _executionTimestamps;
    private readonly int _maxConcurrency;
    private readonly int _maxPerMinute;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _cleanupLock = new();
    private bool _disposed;

    public ExecutionRateLimiter(McpOptions options)
        : this(options, TimeProvider.System)
    {
    }

    internal ExecutionRateLimiter(McpOptions options, TimeProvider timeProvider)
    {
        _maxConcurrency = options.MaxConcurrentExecutions;
        _concurrencySemaphore = new SemaphoreSlim(_maxConcurrency);
        _executionTimestamps = new ConcurrentQueue<DateTimeOffset>();
        _maxPerMinute = options.MaxExecutionsPerMinute;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Number of currently running executions.
    /// </summary>
    public int CurrentConcurrentExecutions => _maxConcurrency - _concurrencySemaphore.CurrentCount;

    /// <summary>
    /// Attempts to acquire a rate limit slot. Returns false if limits are exceeded.
    /// Thread-safe: uses atomic check-and-acquire to prevent TOCTOU race conditions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if slot acquired, false if rate limited</returns>
    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        // Acquire concurrency semaphore first (non-blocking)
        if (!await _concurrencySemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return false;

        // Check per-minute limit while holding semaphore to prevent TOCTOU
        if (!CheckAndRecordExecution())
        {
            _concurrencySemaphore.Release();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Releases a rate limit slot after execution completes.
    /// </summary>
    public void Release()
    {
        _concurrencySemaphore.Release();
    }

    /// <summary>
    /// Atomically checks rate limit and records execution timestamp.
    /// Must be called while holding the concurrency semaphore.
    /// </summary>
    /// <returns>True if under limit and timestamp recorded, false if rate limited</returns>
    private bool CheckAndRecordExecution()
    {
        var now = _timeProvider.GetUtcNow();
        var cutoff = now.AddMinutes(-1);

        lock (_cleanupLock)
        {
            // Clean up old timestamps
            while (_executionTimestamps.TryPeek(out var oldest) && oldest < cutoff)
            {
                _executionTimestamps.TryDequeue(out _);
            }

            // Check if we're under the limit
            if (_executionTimestamps.Count >= _maxPerMinute)
                return false;

            // Record this execution timestamp atomically with the check
            _executionTimestamps.Enqueue(now);
            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _concurrencySemaphore.Dispose();
    }
}

using DraftSpec.Formatters;

namespace DraftSpec.Plugins;

/// <summary>
/// Tracks spec execution statistics progressively for streaming reporters.
/// Thread-safe for use with parallel spec execution.
/// </summary>
public class StreamingStats
{
    private int _total;
    private int _passed;
    private int _failed;
    private int _pending;
    private int _skipped;
    private double _totalDurationMs;

    /// <summary>
    /// Total number of specs processed.
    /// </summary>
    public int Total => _total;

    /// <summary>
    /// Number of passed specs.
    /// </summary>
    public int Passed => _passed;

    /// <summary>
    /// Number of failed specs.
    /// </summary>
    public int Failed => _failed;

    /// <summary>
    /// Number of pending specs.
    /// </summary>
    public int Pending => _pending;

    /// <summary>
    /// Number of skipped specs.
    /// </summary>
    public int Skipped => _skipped;

    /// <summary>
    /// Total duration of all specs in milliseconds.
    /// </summary>
    public double TotalDurationMs => _totalDurationMs;

    /// <summary>
    /// Whether all specs passed (no failures).
    /// </summary>
    public bool Success => _failed == 0;

    /// <summary>
    /// Add a spec result to the statistics.
    /// Thread-safe via Interlocked operations.
    /// </summary>
    public void Add(SpecResult result)
    {
        Interlocked.Increment(ref _total);

        switch (result.Status)
        {
            case SpecStatus.Passed:
                Interlocked.Increment(ref _passed);
                break;
            case SpecStatus.Failed:
                Interlocked.Increment(ref _failed);
                break;
            case SpecStatus.Pending:
                Interlocked.Increment(ref _pending);
                break;
            case SpecStatus.Skipped:
                Interlocked.Increment(ref _skipped);
                break;
        }

        // Thread-safe double addition using CompareExchange
        double initial, computed;
        do
        {
            initial = _totalDurationMs;
            computed = initial + result.Duration.TotalMilliseconds;
        }
        while (Interlocked.CompareExchange(ref _totalDurationMs, computed, initial) != initial);
    }

    /// <summary>
    /// Convert to a SpecSummary for use with formatters.
    /// </summary>
    public SpecSummary ToSummary() => new()
    {
        Total = _total,
        Passed = _passed,
        Failed = _failed,
        Pending = _pending,
        Skipped = _skipped,
        DurationMs = _totalDurationMs
    };

    /// <summary>
    /// Reset all statistics to zero.
    /// </summary>
    public void Reset()
    {
        _total = 0;
        _passed = 0;
        _failed = 0;
        _pending = 0;
        _skipped = 0;
        _totalDurationMs = 0;
    }
}

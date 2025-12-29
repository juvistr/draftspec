using System.Diagnostics;

namespace DraftSpec.Cli;

/// <summary>
/// Abstraction for time-related operations, enabling deterministic testing.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Creates and starts a new stopwatch instance.
    /// </summary>
    IStopwatch StartNew();
}

/// <summary>
/// Abstraction for measuring elapsed time.
/// </summary>
public interface IStopwatch
{
    /// <summary>
    /// Gets the elapsed time since the stopwatch was started.
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Stops the stopwatch.
    /// </summary>
    void Stop();
}

/// <summary>
/// Implementation that delegates to System.Diagnostics.Stopwatch and DateTime.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public IStopwatch StartNew() => new SystemStopwatch();
}

/// <summary>
/// Implementation that wraps System.Diagnostics.Stopwatch.
/// </summary>
public class SystemStopwatch : IStopwatch
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Stop() => _stopwatch.Stop();
}

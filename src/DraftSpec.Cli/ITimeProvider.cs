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

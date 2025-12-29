namespace DraftSpec;

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
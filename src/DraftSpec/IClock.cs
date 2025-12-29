namespace DraftSpec;

/// <summary>
/// Abstraction for time-related operations, enabling deterministic testing.
/// </summary>
public interface IClock
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
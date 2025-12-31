namespace DraftSpec;

/// <summary>
/// Implementation that delegates to System.Diagnostics.Stopwatch and DateTime.
/// </summary>
public class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public IStopwatch StartNew() => new SystemStopwatch();
}

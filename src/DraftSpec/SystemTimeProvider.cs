using System.Diagnostics;

namespace DraftSpec;

/// <summary>
/// Implementation that delegates to System.Diagnostics.Stopwatch and DateTime.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public IStopwatch StartNew() => new SystemStopwatch();
}

/// <summary>
/// Implementation that wraps System.Diagnostics.Stopwatch.
/// </summary>
public class SystemStopwatch : IStopwatch
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <inheritdoc />
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <inheritdoc />
    public void Stop() => _stopwatch.Stop();
}

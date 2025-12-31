using System.Diagnostics;

namespace DraftSpec;

/// <summary>
/// Implementation that wraps System.Diagnostics.Stopwatch.
/// </summary>
public class SystemStopwatch : IStopwatch
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <inheritdoc />
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <inheritdoc />
    public void StopTiming() => _stopwatch.Stop();
}
using System.Diagnostics;

namespace DraftSpec.Cli;

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

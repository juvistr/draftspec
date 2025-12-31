namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock stopwatch for testing.
/// Returns a configurable elapsed time.
/// </summary>
public class MockStopwatch : IStopwatch
{
    private readonly int _milliseconds;

    public MockStopwatch(int milliseconds = 100)
    {
        _milliseconds = milliseconds;
    }

    public TimeSpan Elapsed => TimeSpan.FromMilliseconds(_milliseconds);

    public void StopTiming() { }
}

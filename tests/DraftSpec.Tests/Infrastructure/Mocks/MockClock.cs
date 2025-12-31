namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock time provider for testing.
/// Allows configuring the current time and elapsed stopwatch time.
/// </summary>
public class MockClock : IClock
{
    /// <summary>
    /// The UTC time to return from UtcNow.
    /// </summary>
    public DateTime CurrentUtcNow { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The elapsed milliseconds to return from stopwatches created by this clock.
    /// </summary>
    public int ElapsedMilliseconds { get; set; } = 100;

    public DateTime UtcNow => CurrentUtcNow;

    public IStopwatch StartNew() => new MockStopwatch(ElapsedMilliseconds);
}

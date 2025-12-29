using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SystemTimeProvider and SystemStopwatch classes.
/// </summary>
public class TimeProviderTests
{
    [Test]
    public async Task UtcNow_ReturnsReasonableTime()
    {
        var provider = new SystemTimeProvider();
        var now = provider.UtcNow;
        var expected = DateTime.UtcNow;
        await Assert.That(Math.Abs((now - expected).TotalSeconds)).IsLessThan(1);
    }

    [Test]
    public async Task StartNew_ReturnsWorkingStopwatch()
    {
        var provider = new SystemTimeProvider();
        var stopwatch = provider.StartNew();

        await Assert.That(stopwatch).IsNotNull();
        await Assert.That(stopwatch).IsTypeOf<SystemStopwatch>();
    }

    [Test]
    public async Task Stopwatch_Elapsed_IncreasesOverTime()
    {
        var provider = new SystemTimeProvider();
        var stopwatch = provider.StartNew();

        var initialElapsed = stopwatch.Elapsed;
        await Task.Delay(50);
        var laterElapsed = stopwatch.Elapsed;

        await Assert.That(laterElapsed).IsGreaterThan(initialElapsed);
        await Assert.That(laterElapsed.TotalMilliseconds).IsGreaterThanOrEqualTo(40);
    }

    [Test]
    public async Task Stopwatch_Stop_FreezesElapsed()
    {
        var provider = new SystemTimeProvider();
        var stopwatch = provider.StartNew();

        await Task.Delay(20);
        stopwatch.Stop();
        var elapsedAfterStop = stopwatch.Elapsed;

        await Task.Delay(50);
        var elapsedLater = stopwatch.Elapsed;

        // After Stop(), Elapsed should not change significantly
        var difference = Math.Abs((elapsedLater - elapsedAfterStop).TotalMilliseconds);
        await Assert.That(difference).IsLessThan(5);
    }
}

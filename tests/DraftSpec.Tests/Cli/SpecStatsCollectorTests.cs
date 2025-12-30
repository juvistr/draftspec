using DraftSpec.Cli;
using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SpecStatsCollector service.
/// </summary>
public class SpecStatsCollectorTests
{
    [Test]
    public async Task CollectAsync_WithNoFiles_ReturnsZeroStats()
    {
        var collector = new SpecStatsCollector();

        var stats = await collector.CollectAsync([], "/some/path");

        await Assert.That(stats.Total).IsEqualTo(0);
        await Assert.That(stats.Regular).IsEqualTo(0);
        await Assert.That(stats.Focused).IsEqualTo(0);
        await Assert.That(stats.Skipped).IsEqualTo(0);
        await Assert.That(stats.Pending).IsEqualTo(0);
        await Assert.That(stats.HasFocusMode).IsFalse();
        await Assert.That(stats.FileCount).IsEqualTo(0);
    }

    [Test]
    public async Task SpecStats_Record_HasCorrectProperties()
    {
        var stats = new SpecStats(
            Total: 10,
            Regular: 5,
            Focused: 2,
            Skipped: 1,
            Pending: 2,
            HasFocusMode: true,
            FileCount: 3);

        await Assert.That(stats.Total).IsEqualTo(10);
        await Assert.That(stats.Regular).IsEqualTo(5);
        await Assert.That(stats.Focused).IsEqualTo(2);
        await Assert.That(stats.Skipped).IsEqualTo(1);
        await Assert.That(stats.Pending).IsEqualTo(2);
        await Assert.That(stats.HasFocusMode).IsTrue();
        await Assert.That(stats.FileCount).IsEqualTo(3);
    }

    [Test]
    public async Task SpecStats_HasFocusMode_TrueWhenFocusedGreaterThanZero()
    {
        var statsWithFocus = new SpecStats(10, 8, 2, 0, 0, true, 1);
        var statsWithoutFocus = new SpecStats(10, 10, 0, 0, 0, false, 1);

        await Assert.That(statsWithFocus.HasFocusMode).IsTrue();
        await Assert.That(statsWithoutFocus.HasFocusMode).IsFalse();
    }
}

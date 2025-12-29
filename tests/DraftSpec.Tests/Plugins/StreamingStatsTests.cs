using DraftSpec.Plugins;

namespace DraftSpec.Tests.Plugins;

public class StreamingStatsTests
{
    #region Add_IncrementsByStatus

    [Test]
    public async Task Add_PassedSpec_IncrementsPassed()
    {
        var stats = new StreamingStats();
        var result = CreateResult(SpecStatus.Passed);

        stats.Add(result);

        await Assert.That(stats.Passed).IsEqualTo(1);
    }

    [Test]
    public async Task Add_FailedSpec_IncrementsFailed()
    {
        var stats = new StreamingStats();
        var result = CreateResult(SpecStatus.Failed);

        stats.Add(result);

        await Assert.That(stats.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task Add_PendingSpec_IncrementsPending()
    {
        var stats = new StreamingStats();
        var result = CreateResult(SpecStatus.Pending);

        stats.Add(result);

        await Assert.That(stats.Pending).IsEqualTo(1);
    }

    [Test]
    public async Task Add_SkippedSpec_IncrementsSkipped()
    {
        var stats = new StreamingStats();
        var result = CreateResult(SpecStatus.Skipped);

        stats.Add(result);

        await Assert.That(stats.Skipped).IsEqualTo(1);
    }

    #endregion

    #region Add_Duration

    [Test]
    public async Task Add_AccumulatesDuration()
    {
        var stats = new StreamingStats();
        var result1 = CreateResult(SpecStatus.Passed, TimeSpan.FromMilliseconds(100));
        var result2 = CreateResult(SpecStatus.Passed, TimeSpan.FromMilliseconds(200));

        stats.Add(result1);
        stats.Add(result2);

        await Assert.That(stats.TotalDurationMs).IsEqualTo(300);
    }

    #endregion

    #region Add_Total

    [Test]
    public async Task Add_IncrementsTotal()
    {
        var stats = new StreamingStats();
        var result1 = CreateResult(SpecStatus.Passed);
        var result2 = CreateResult(SpecStatus.Failed);
        var result3 = CreateResult(SpecStatus.Pending);

        stats.Add(result1);
        stats.Add(result2);
        stats.Add(result3);

        await Assert.That(stats.Total).IsEqualTo(3);
    }

    #endregion

    #region Success

    [Test]
    public async Task Success_TrueWhenNoFailures()
    {
        var stats = new StreamingStats();
        stats.Add(CreateResult(SpecStatus.Passed));
        stats.Add(CreateResult(SpecStatus.Pending));
        stats.Add(CreateResult(SpecStatus.Skipped));

        await Assert.That(stats.Success).IsTrue();
    }

    [Test]
    public async Task Success_FalseWhenHasFailures()
    {
        var stats = new StreamingStats();
        stats.Add(CreateResult(SpecStatus.Passed));
        stats.Add(CreateResult(SpecStatus.Failed));

        await Assert.That(stats.Success).IsFalse();
    }

    #endregion

    #region ToSummary

    [Test]
    public async Task ToSummary_ReturnsCorrectValues()
    {
        var stats = new StreamingStats();
        stats.Add(CreateResult(SpecStatus.Passed, TimeSpan.FromMilliseconds(50)));
        stats.Add(CreateResult(SpecStatus.Passed, TimeSpan.FromMilliseconds(50)));
        stats.Add(CreateResult(SpecStatus.Failed, TimeSpan.FromMilliseconds(25)));
        stats.Add(CreateResult(SpecStatus.Pending));
        stats.Add(CreateResult(SpecStatus.Skipped));

        var summary = stats.ToSummary();

        await Assert.That(summary.Total).IsEqualTo(5);
        await Assert.That(summary.Passed).IsEqualTo(2);
        await Assert.That(summary.Failed).IsEqualTo(1);
        await Assert.That(summary.Pending).IsEqualTo(1);
        await Assert.That(summary.Skipped).IsEqualTo(1);
        await Assert.That(summary.DurationMs).IsEqualTo(125);
    }

    #endregion

    #region Reset

    [Test]
    public async Task Reset_ClearsAllCounters()
    {
        var stats = new StreamingStats();
        stats.Add(CreateResult(SpecStatus.Passed, TimeSpan.FromMilliseconds(100)));
        stats.Add(CreateResult(SpecStatus.Failed, TimeSpan.FromMilliseconds(50)));
        stats.Add(CreateResult(SpecStatus.Pending));
        stats.Add(CreateResult(SpecStatus.Skipped));

        stats.Reset();

        await Assert.That(stats.Total).IsEqualTo(0);
        await Assert.That(stats.Passed).IsEqualTo(0);
        await Assert.That(stats.Failed).IsEqualTo(0);
        await Assert.That(stats.Pending).IsEqualTo(0);
        await Assert.That(stats.Skipped).IsEqualTo(0);
        await Assert.That(stats.TotalDurationMs).IsEqualTo(0);
    }

    #endregion

    private static SpecResult CreateResult(SpecStatus status, TimeSpan duration = default)
    {
        var spec = new SpecDefinition("test spec", () => { });
        return new SpecResult(spec, status, ["context"], duration);
    }
}

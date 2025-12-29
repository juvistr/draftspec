using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for coverage threshold checking.
/// </summary>
public class CoverageThresholdCheckerTests
{
    private readonly CoverageThresholdChecker _checker = new();

    #region Line Threshold

    [Test]
    public async Task Check_LineThresholdMet_Passes()
    {
        var report = CreateReport(coveredLines: 80, totalLines: 100);
        var thresholds = new ThresholdsConfig { Line = 80 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsTrue();
        await Assert.That(result.Failures).IsEmpty();
    }

    [Test]
    public async Task Check_LineThresholdExceeded_Passes()
    {
        var report = CreateReport(coveredLines: 90, totalLines: 100);
        var thresholds = new ThresholdsConfig { Line = 80 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsTrue();
    }

    [Test]
    public async Task Check_LineThresholdNotMet_Fails()
    {
        var report = CreateReport(coveredLines: 70, totalLines: 100);
        var thresholds = new ThresholdsConfig { Line = 80 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsFalse();
        await Assert.That(result.Failures).Count().IsEqualTo(1);
        await Assert.That(result.Failures[0]).Contains("Line coverage");
        await Assert.That(result.Failures[0]).Contains("70.0%");
        await Assert.That(result.Failures[0]).Contains("80%");
    }

    #endregion

    #region Branch Threshold

    [Test]
    public async Task Check_BranchThresholdMet_Passes()
    {
        var report = CreateReport(coveredBranches: 70, totalBranches: 100);
        var thresholds = new ThresholdsConfig { Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsTrue();
    }

    [Test]
    public async Task Check_BranchThresholdNotMet_Fails()
    {
        var report = CreateReport(coveredBranches: 60, totalBranches: 100);
        var thresholds = new ThresholdsConfig { Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsFalse();
        await Assert.That(result.Failures[0]).Contains("Branch coverage");
        await Assert.That(result.Failures[0]).Contains("60.0%");
        await Assert.That(result.Failures[0]).Contains("70%");
    }

    #endregion

    #region Combined Thresholds

    [Test]
    public async Task Check_BothThresholdsMet_Passes()
    {
        var report = CreateReport(
            coveredLines: 85, totalLines: 100,
            coveredBranches: 75, totalBranches: 100);
        var thresholds = new ThresholdsConfig { Line = 80, Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsTrue();
        await Assert.That(result.Failures).IsEmpty();
    }

    [Test]
    public async Task Check_OnlyLineThresholdNotMet_FailsWithOneMessage()
    {
        var report = CreateReport(
            coveredLines: 70, totalLines: 100,
            coveredBranches: 80, totalBranches: 100);
        var thresholds = new ThresholdsConfig { Line = 80, Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsFalse();
        await Assert.That(result.Failures).Count().IsEqualTo(1);
        await Assert.That(result.Failures[0]).Contains("Line");
    }

    [Test]
    public async Task Check_OnlyBranchThresholdNotMet_FailsWithOneMessage()
    {
        var report = CreateReport(
            coveredLines: 90, totalLines: 100,
            coveredBranches: 50, totalBranches: 100);
        var thresholds = new ThresholdsConfig { Line = 80, Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsFalse();
        await Assert.That(result.Failures).Count().IsEqualTo(1);
        await Assert.That(result.Failures[0]).Contains("Branch");
    }

    [Test]
    public async Task Check_BothThresholdsNotMet_FailsWithTwoMessages()
    {
        var report = CreateReport(
            coveredLines: 50, totalLines: 100,
            coveredBranches: 40, totalBranches: 100);
        var thresholds = new ThresholdsConfig { Line = 80, Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsFalse();
        await Assert.That(result.Failures).Count().IsEqualTo(2);
    }

    #endregion

    #region Result Values

    [Test]
    public async Task Check_SetsActualPercentages()
    {
        var report = CreateReport(
            coveredLines: 75, totalLines: 100,
            coveredBranches: 60, totalBranches: 100);
        var thresholds = new ThresholdsConfig { Line = 80, Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.ActualLinePercent).IsEqualTo(75.0);
        await Assert.That(result.ActualBranchPercent).IsEqualTo(60.0);
    }

    [Test]
    public async Task Check_SetsRequiredPercentages()
    {
        var report = CreateReport(coveredLines: 50, totalLines: 100);
        var thresholds = new ThresholdsConfig { Line = 80, Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.RequiredLinePercent).IsEqualTo(80);
        await Assert.That(result.RequiredBranchPercent).IsEqualTo(70);
    }

    [Test]
    public async Task Check_NoThresholdsConfigured_Passes()
    {
        var report = CreateReport(coveredLines: 10, totalLines: 100);
        var thresholds = new ThresholdsConfig(); // No thresholds set

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsTrue();
    }

    [Test]
    public async Task FailureMessage_CombinesAllFailures()
    {
        var report = CreateReport(
            coveredLines: 50, totalLines: 100,
            coveredBranches: 40, totalBranches: 100);
        var thresholds = new ThresholdsConfig { Line = 80, Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.FailureMessage).Contains("Line");
        await Assert.That(result.FailureMessage).Contains("Branch");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Check_ZeroTotalLines_ReturnsZeroPercent()
    {
        var report = CreateReport(coveredLines: 0, totalLines: 0);
        var thresholds = new ThresholdsConfig { Line = 80 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.ActualLinePercent).IsEqualTo(0);
        await Assert.That(result.Passed).IsFalse();
    }

    [Test]
    public async Task Check_ZeroTotalBranches_ReturnsZeroPercent()
    {
        var report = CreateReport(
            coveredLines: 100, totalLines: 100,
            coveredBranches: 0, totalBranches: 0);
        var thresholds = new ThresholdsConfig { Branch = 70 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.ActualBranchPercent).IsEqualTo(0);
        await Assert.That(result.Passed).IsFalse();
    }

    [Test]
    public async Task Check_ExactlyAtThreshold_Passes()
    {
        var report = CreateReport(coveredLines: 80, totalLines: 100);
        var thresholds = new ThresholdsConfig { Line = 80.0 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsTrue();
    }

    [Test]
    public async Task Check_JustBelowThreshold_Fails()
    {
        // 79.99% coverage
        var report = CreateReport(coveredLines: 7999, totalLines: 10000);
        var thresholds = new ThresholdsConfig { Line = 80.0 };

        var result = _checker.Check(report, thresholds);

        await Assert.That(result.Passed).IsFalse();
    }

    #endregion

    #region Helper Methods

    private static CoverageReport CreateReport(
        int coveredLines = 0,
        int totalLines = 0,
        int coveredBranches = 0,
        int totalBranches = 0)
    {
        return new CoverageReport
        {
            Summary = new CoverageSummary
            {
                CoveredLines = coveredLines,
                TotalLines = totalLines,
                CoveredBranches = coveredBranches,
                TotalBranches = totalBranches
            }
        };
    }

    #endregion
}

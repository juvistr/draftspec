using DraftSpec.Cli;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for InProcessRunResult and InProcessRunSummary records.
/// </summary>
public class InProcessRunSummaryTests
{
    #region InProcessRunResult

    [Test]
    public async Task RunResult_Success_WhenNoErrorAndNoFailures()
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary { Total = 5, Passed = 5, Failed = 0 }
        };
        var result = new InProcessRunResult("test.spec.csx", report, TimeSpan.FromSeconds(1));

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task RunResult_NotSuccess_WhenHasError()
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary { Total = 5, Passed = 5, Failed = 0 }
        };
        var result = new InProcessRunResult("test.spec.csx", report, TimeSpan.FromSeconds(1),
            new Exception("Compilation failed"));

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task RunResult_NotSuccess_WhenHasFailedSpecs()
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary { Total = 5, Passed = 4, Failed = 1 }
        };
        var result = new InProcessRunResult("test.spec.csx", report, TimeSpan.FromSeconds(1));

        await Assert.That(result.Success).IsFalse();
    }

    #endregion

    #region InProcessRunSummary Success

    [Test]
    public async Task Summary_Success_WhenAllResultsSucceed()
    {
        var results = new List<InProcessRunResult>
        {
            CreateSuccessResult("a.spec.csx", passed: 3),
            CreateSuccessResult("b.spec.csx", passed: 5)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(2));

        await Assert.That(summary.Success).IsTrue();
    }

    [Test]
    public async Task Summary_NotSuccess_WhenAnyResultFails()
    {
        var results = new List<InProcessRunResult>
        {
            CreateSuccessResult("a.spec.csx", passed: 3),
            CreateFailedResult("b.spec.csx", passed: 4, failed: 1)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(2));

        await Assert.That(summary.Success).IsFalse();
    }

    [Test]
    public async Task Summary_NotSuccess_WhenAnyResultHasError()
    {
        var results = new List<InProcessRunResult>
        {
            CreateSuccessResult("a.spec.csx", passed: 3),
            CreateErrorResult("b.spec.csx", new Exception("Compile error"))
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(2));

        await Assert.That(summary.Success).IsFalse();
    }

    #endregion

    #region Aggregation

    [Test]
    public async Task Summary_TotalSpecs_SumsAllResults()
    {
        var results = new List<InProcessRunResult>
        {
            CreateResult(total: 10, passed: 10),
            CreateResult(total: 15, passed: 15),
            CreateResult(total: 5, passed: 5)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(3));

        await Assert.That(summary.TotalSpecs).IsEqualTo(30);
    }

    [Test]
    public async Task Summary_Passed_SumsAllResults()
    {
        var results = new List<InProcessRunResult>
        {
            CreateResult(total: 10, passed: 8, failed: 2),
            CreateResult(total: 5, passed: 4, failed: 1)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(2));

        await Assert.That(summary.Passed).IsEqualTo(12);
    }

    [Test]
    public async Task Summary_Failed_SumsAllResults()
    {
        var results = new List<InProcessRunResult>
        {
            CreateResult(total: 10, passed: 8, failed: 2),
            CreateResult(total: 5, passed: 3, failed: 2)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(2));

        await Assert.That(summary.Failed).IsEqualTo(4);
    }

    [Test]
    public async Task Summary_Pending_SumsAllResults()
    {
        var results = new List<InProcessRunResult>
        {
            CreateResult(total: 10, passed: 8, pending: 2),
            CreateResult(total: 5, passed: 3, pending: 2)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(2));

        await Assert.That(summary.Pending).IsEqualTo(4);
    }

    [Test]
    public async Task Summary_Skipped_SumsAllResults()
    {
        var results = new List<InProcessRunResult>
        {
            CreateResult(total: 10, passed: 7, skipped: 3),
            CreateResult(total: 5, passed: 4, skipped: 1)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(2));

        await Assert.That(summary.Skipped).IsEqualTo(4);
    }

    [Test]
    public async Task Summary_MixedStats_AggregatesCorrectly()
    {
        var results = new List<InProcessRunResult>
        {
            CreateResult(total: 20, passed: 15, failed: 2, pending: 2, skipped: 1),
            CreateResult(total: 10, passed: 7, failed: 1, pending: 1, skipped: 1)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(5));

        await Assert.That(summary.TotalSpecs).IsEqualTo(30);
        await Assert.That(summary.Passed).IsEqualTo(22);
        await Assert.That(summary.Failed).IsEqualTo(3);
        await Assert.That(summary.Pending).IsEqualTo(3);
        await Assert.That(summary.Skipped).IsEqualTo(2);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Summary_EmptyResults_ReturnsZeros()
    {
        var summary = new InProcessRunSummary([], TimeSpan.Zero);

        await Assert.That(summary.TotalSpecs).IsEqualTo(0);
        await Assert.That(summary.Passed).IsEqualTo(0);
        await Assert.That(summary.Failed).IsEqualTo(0);
        await Assert.That(summary.Pending).IsEqualTo(0);
        await Assert.That(summary.Skipped).IsEqualTo(0);
        await Assert.That(summary.Success).IsTrue();
    }

    [Test]
    public async Task Summary_SingleResult_ReturnsItsValues()
    {
        var results = new List<InProcessRunResult>
        {
            CreateResult(total: 42, passed: 40, failed: 2)
        };
        var summary = new InProcessRunSummary(results, TimeSpan.FromSeconds(10));

        await Assert.That(summary.TotalSpecs).IsEqualTo(42);
        await Assert.That(summary.Passed).IsEqualTo(40);
        await Assert.That(summary.Failed).IsEqualTo(2);
    }

    [Test]
    public async Task Summary_TotalDuration_IsPreserved()
    {
        var duration = TimeSpan.FromMinutes(2.5);
        var summary = new InProcessRunSummary([], duration);

        await Assert.That(summary.TotalDuration).IsEqualTo(duration);
    }

    #endregion

    #region Helper Methods

    private static InProcessRunResult CreateSuccessResult(string file, int passed)
    {
        return CreateResult(file, passed, passed, 0, 0, 0, null);
    }

    private static InProcessRunResult CreateFailedResult(string file, int passed, int failed)
    {
        return CreateResult(file, passed + failed, passed, failed, 0, 0, null);
    }

    private static InProcessRunResult CreateErrorResult(string file, Exception error)
    {
        var report = new SpecReport { Summary = new SpecSummary() };
        return new InProcessRunResult(file, report, TimeSpan.FromSeconds(1), error);
    }

    private static InProcessRunResult CreateResult(
        int total, int passed, int failed = 0, int pending = 0, int skipped = 0)
    {
        return CreateResult("test.spec.csx", total, passed, failed, pending, skipped, null);
    }

    private static InProcessRunResult CreateResult(
        string file, int total, int passed, int failed, int pending, int skipped, Exception? error)
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary
            {
                Total = total,
                Passed = passed,
                Failed = failed,
                Pending = pending,
                Skipped = skipped
            }
        };
        return new InProcessRunResult(file, report, TimeSpan.FromSeconds(1), error);
    }

    #endregion
}

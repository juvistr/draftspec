using DraftSpec.Cli;
using DraftSpec.Formatters;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for report merging logic.
/// </summary>
public class ReportMergerTests
{
    #region Empty Input

    [Test]
    public async Task Merge_EmptyList_ReturnsEmptyReport()
    {
        var result = ReportMerger.Merge([], "/test/source");

        await Assert.That(result.Source).IsEqualTo("/test/source");
        await Assert.That(result.Summary.Total).IsEqualTo(0);
        await Assert.That(result.Contexts).IsEmpty();
    }

    [Test]
    public async Task Merge_ListWithOnlyWhitespace_ReturnsEmptyReport()
    {
        var result = ReportMerger.Merge(["", "  ", "\n"], "/test/source");

        await Assert.That(result.Summary.Total).IsEqualTo(0);
        await Assert.That(result.Contexts).IsEmpty();
    }

    #endregion

    #region Single Report

    [Test]
    public async Task Merge_SingleReport_UpdatesSource()
    {
        var json = CreateReportJson(passed: 2, failed: 1, source: "original");

        var result = ReportMerger.Merge([json], "/new/source");

        await Assert.That(result.Source).IsEqualTo("/new/source");
        await Assert.That(result.Summary.Passed).IsEqualTo(2);
        await Assert.That(result.Summary.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task Merge_SingleReport_PreservesContexts()
    {
        var json = CreateReportJson(passed: 1, contextDescription: "Calculator");

        var result = ReportMerger.Merge([json], "/source");

        await Assert.That(result.Contexts.Count).IsEqualTo(1);
        await Assert.That(result.Contexts[0].Description).IsEqualTo("Calculator");
    }

    #endregion

    #region Multiple Reports

    [Test]
    public async Task Merge_MultipleReports_SumsTotals()
    {
        var json1 = CreateReportJson(passed: 3, failed: 1);
        var json2 = CreateReportJson(passed: 2, failed: 0);

        var result = ReportMerger.Merge([json1, json2], "/source");

        await Assert.That(result.Summary.Passed).IsEqualTo(5);
        await Assert.That(result.Summary.Failed).IsEqualTo(1);
        await Assert.That(result.Summary.Total).IsEqualTo(6);
    }

    [Test]
    public async Task Merge_MultipleReports_CombinesContexts()
    {
        var json1 = CreateReportJson(passed: 1, contextDescription: "Calculator");
        var json2 = CreateReportJson(passed: 1, contextDescription: "Parser");

        var result = ReportMerger.Merge([json1, json2], "/source");

        await Assert.That(result.Contexts.Count).IsEqualTo(2);
        var descriptions = result.Contexts.Select(c => c.Description).ToList();
        await Assert.That(descriptions).Contains("Calculator");
        await Assert.That(descriptions).Contains("Parser");
    }

    [Test]
    public async Task Merge_MultipleReports_SumsDuration()
    {
        var json1 = CreateReportJson(passed: 1, durationMs: 100);
        var json2 = CreateReportJson(passed: 1, durationMs: 200);

        var result = ReportMerger.Merge([json1, json2], "/source");

        await Assert.That(result.Summary.DurationMs).IsEqualTo(300);
    }

    [Test]
    public async Task Merge_MultipleReports_UsesEarliestTimestamp()
    {
        var earlier = DateTime.UtcNow.AddMinutes(-10);
        var later = DateTime.UtcNow;

        var json1 = CreateReportJson(passed: 1, timestamp: later);
        var json2 = CreateReportJson(passed: 1, timestamp: earlier);

        var result = ReportMerger.Merge([json1, json2], "/source");

        await Assert.That(result.Timestamp).IsEqualTo(earlier);
    }

    [Test]
    public async Task Merge_MultipleReports_SumsAllStatuses()
    {
        var json1 = CreateReportJson(passed: 2, failed: 1, pending: 1, skipped: 0);
        var json2 = CreateReportJson(passed: 1, failed: 2, pending: 0, skipped: 1);

        var result = ReportMerger.Merge([json1, json2], "/source");

        await Assert.That(result.Summary.Passed).IsEqualTo(3);
        await Assert.That(result.Summary.Failed).IsEqualTo(3);
        await Assert.That(result.Summary.Pending).IsEqualTo(1);
        await Assert.That(result.Summary.Skipped).IsEqualTo(1);
    }

    #endregion

    #region Mixed Valid/Invalid

    [Test]
    public async Task Merge_MixedValidAndWhitespace_IgnoresWhitespace()
    {
        var json = CreateReportJson(passed: 5);

        var result = ReportMerger.Merge(["", json, "  "], "/source");

        await Assert.That(result.Summary.Passed).IsEqualTo(5);
    }

    #endregion

    #region Helpers

    private static string CreateReportJson(
        int passed = 0,
        int failed = 0,
        int pending = 0,
        int skipped = 0,
        double durationMs = 0,
        string source = "test",
        string contextDescription = "Test Context",
        DateTime? timestamp = null)
    {
        var ts = timestamp ?? DateTime.UtcNow;
        var total = passed + failed + pending + skipped;

        var report = new SpecReport
        {
            Timestamp = ts,
            Source = source,
            Summary = new SpecSummary
            {
                Total = total,
                Passed = passed,
                Failed = failed,
                Pending = pending,
                Skipped = skipped,
                DurationMs = durationMs
            },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = contextDescription,
                    Specs = [],
                    Contexts = []
                }
            ]
        };

        return report.ToJson();
    }

    #endregion
}

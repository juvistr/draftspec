using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SpecReport JSON serialization and merging functionality.
/// </summary>
public class ReportMergingTests
{
    #region ToJson/FromJson Round-Trip Tests

    [Test]
    public async Task ToJson_ProducesValidJson()
    {
        var report = new SpecReport
        {
            Timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Source = "/test/path",
            Summary = new SpecSummary { Total = 5, Passed = 3, Failed = 1, Pending = 1 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Test Context",
                    Specs =
                    [
                        new SpecResultReport { Description = "spec 1", Status = "passed" }
                    ]
                }
            ]
        };

        var json = report.ToJson();

        await Assert.That(json).Contains("\"source\"");
        await Assert.That(json).Contains("\"summary\"");
        await Assert.That(json).Contains("\"contexts\"");
        await Assert.That(json).Contains("Test Context");
    }

    [Test]
    public async Task FromJson_ToJson_RoundTrip_PreservesData()
    {
        var original = new SpecReport
        {
            Timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Source = "/test/path",
            Summary = new SpecSummary
            {
                Total = 10,
                Passed = 7,
                Failed = 2,
                Pending = 1,
                Skipped = 0,
                DurationMs = 1234.5
            },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Context A",
                    Specs =
                    [
                        new SpecResultReport { Description = "spec 1", Status = "passed", DurationMs = 10 },
                        new SpecResultReport { Description = "spec 2", Status = "failed", Error = "Test error" }
                    ],
                    Contexts =
                    [
                        new SpecContextReport
                        {
                            Description = "Nested Context",
                            Specs =
                            [
                                new SpecResultReport { Description = "nested spec", Status = "pending" }
                            ]
                        }
                    ]
                }
            ]
        };

        var json = original.ToJson();
        var restored = SpecReport.FromJson(json);

        await Assert.That(restored.Source).IsEqualTo(original.Source);
        await Assert.That(restored.Summary.Total).IsEqualTo(10);
        await Assert.That(restored.Summary.Passed).IsEqualTo(7);
        await Assert.That(restored.Summary.Failed).IsEqualTo(2);
        await Assert.That(restored.Contexts).Count().IsEqualTo(1);
        await Assert.That(restored.Contexts[0].Description).IsEqualTo("Context A");
        await Assert.That(restored.Contexts[0].Specs).Count().IsEqualTo(2);
        await Assert.That(restored.Contexts[0].Contexts).Count().IsEqualTo(1);
    }

    #endregion

    #region SpecSummary Aggregation Tests

    [Test]
    public async Task SpecSummary_Success_ReturnsTrueWhenNoFailures()
    {
        var summary = new SpecSummary { Total = 10, Passed = 8, Failed = 0, Pending = 2 };

        await Assert.That(summary.Success).IsTrue();
    }

    [Test]
    public async Task SpecSummary_Success_ReturnsFalseWhenHasFailures()
    {
        var summary = new SpecSummary { Total = 10, Passed = 7, Failed = 3 };

        await Assert.That(summary.Success).IsFalse();
    }

    #endregion

    #region SpecResultReport Status Properties

    [Test]
    public async Task SpecResultReport_Passed_ReturnsTrueForPassedStatus()
    {
        var result = new SpecResultReport { Status = "passed" };

        await Assert.That(result.Passed).IsTrue();
        await Assert.That(result.Failed).IsFalse();
        await Assert.That(result.Pending).IsFalse();
        await Assert.That(result.Skipped).IsFalse();
    }

    [Test]
    public async Task SpecResultReport_Failed_ReturnsTrueForFailedStatus()
    {
        var result = new SpecResultReport { Status = "failed" };

        await Assert.That(result.Failed).IsTrue();
        await Assert.That(result.Passed).IsFalse();
    }

    [Test]
    public async Task SpecResultReport_Pending_ReturnsTrueForPendingStatus()
    {
        var result = new SpecResultReport { Status = "pending" };

        await Assert.That(result.Pending).IsTrue();
    }

    [Test]
    public async Task SpecResultReport_Skipped_ReturnsTrueForSkippedStatus()
    {
        var result = new SpecResultReport { Status = "skipped" };

        await Assert.That(result.Skipped).IsTrue();
    }

    #endregion

    #region Report Merging Simulation Tests

    [Test]
    public async Task MergeReports_SingleReport_PreservesAllData()
    {
        var report = new SpecReport
        {
            Source = "/original/path",
            Summary = new SpecSummary { Total = 5, Passed = 5 },
            Contexts = [new SpecContextReport { Description = "Context" }]
        };

        // Simulate what RunCommand.MergeReports does for single report
        var json = report.ToJson();
        var restored = SpecReport.FromJson(json);
        restored.Source = "/new/path";

        await Assert.That(restored.Source).IsEqualTo("/new/path");
        await Assert.That(restored.Summary.Total).IsEqualTo(5);
        await Assert.That(restored.Contexts).Count().IsEqualTo(1);
    }

    [Test]
    public async Task MergeReports_MultipleReports_AggregatesSummary()
    {
        var report1 = new SpecReport
        {
            Timestamp = new DateTime(2025, 1, 1),
            Summary = new SpecSummary { Total = 5, Passed = 4, Failed = 1, DurationMs = 100 },
            Contexts = [new SpecContextReport { Description = "File1" }]
        };

        var report2 = new SpecReport
        {
            Timestamp = new DateTime(2025, 1, 2),
            Summary = new SpecSummary { Total = 3, Passed = 2, Failed = 0, Pending = 1, DurationMs = 50 },
            Contexts = [new SpecContextReport { Description = "File2" }]
        };

        // Simulate merging logic
        var reports = new[] { report1, report2 };
        var combined = new SpecReport
        {
            Timestamp = reports.Min(r => r.Timestamp),
            Source = "/combined",
            Contexts = reports.SelectMany(r => r.Contexts).ToList(),
            Summary = new SpecSummary
            {
                Total = reports.Sum(r => r.Summary.Total),
                Passed = reports.Sum(r => r.Summary.Passed),
                Failed = reports.Sum(r => r.Summary.Failed),
                Pending = reports.Sum(r => r.Summary.Pending),
                Skipped = reports.Sum(r => r.Summary.Skipped),
                DurationMs = reports.Sum(r => r.Summary.DurationMs)
            }
        };

        await Assert.That(combined.Summary.Total).IsEqualTo(8);
        await Assert.That(combined.Summary.Passed).IsEqualTo(6);
        await Assert.That(combined.Summary.Failed).IsEqualTo(1);
        await Assert.That(combined.Summary.Pending).IsEqualTo(1);
        await Assert.That(combined.Summary.DurationMs).IsEqualTo(150);
        await Assert.That(combined.Contexts).Count().IsEqualTo(2);
        await Assert.That(combined.Timestamp).IsEqualTo(new DateTime(2025, 1, 1));
    }

    [Test]
    public async Task MergeReports_EmptyList_ReturnsEmptyReport()
    {
        var reports = Array.Empty<SpecReport>();

        // Simulate merging logic for empty input
        var combined = reports.Length == 0
            ? new SpecReport { Timestamp = DateTime.UtcNow, Summary = new SpecSummary(), Contexts = [] }
            : throw new InvalidOperationException();

        await Assert.That(combined.Summary.Total).IsEqualTo(0);
        await Assert.That(combined.Contexts).IsEmpty();
    }

    #endregion

    #region JSON Format Validation

    [Test]
    public async Task ToJson_UseCamelCase()
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary { DurationMs = 100 }
        };

        var json = report.ToJson();

        await Assert.That(json).Contains("\"durationMs\"");
        await Assert.That(json).DoesNotContain("\"DurationMs\"");
    }

    [Test]
    public async Task ToJson_OmitsNullValues()
    {
        var report = new SpecReport
        {
            Source = null,
            Summary = new SpecSummary(),
            Contexts = []
        };

        var json = report.ToJson();

        await Assert.That(json).DoesNotContain("\"source\"");
    }

    [Test]
    public async Task ToJson_FormatsIndented()
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary { Total = 1 }
        };

        var json = report.ToJson();

        // Indented JSON has newlines
        await Assert.That(json).Contains("\n");
    }

    #endregion
}
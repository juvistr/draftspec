using System.Text.Json;
using DraftSpec.Cli;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for result aggregation and summary computation.
/// These tests verify mathematical invariants that must hold for all inputs.
/// </summary>
public class ResultAggregationPropertyTests
{
    [Test]
    public void SpecSummary_SumIdentity()
    {
        // Property: Passed + Failed + Pending + Skipped should equal Total
        // when summary is constructed correctly
        Prop.ForAll<int, int, int>((passed, failed, pending) =>
        {
            // Use absolute values to ensure non-negative
            var p = Math.Abs(passed % 100);
            var f = Math.Abs(failed % 100);
            var pe = Math.Abs(pending % 100);
            var s = Math.Abs((passed + failed) % 100); // Derive skipped

            var summary = new SpecSummary
            {
                Passed = p,
                Failed = f,
                Pending = pe,
                Skipped = s,
                Total = p + f + pe + s
            };

            return summary.Passed + summary.Failed + summary.Pending + summary.Skipped == summary.Total;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void SpecSummary_SuccessProperty()
    {
        // Property: Success is true if and only if Failed == 0
        Prop.ForAll<int, int>((passed, failed) =>
        {
            var summary = new SpecSummary
            {
                Passed = Math.Abs(passed % 1000),
                Failed = Math.Abs(failed % 1000)
            };

            return summary.Success == (summary.Failed == 0);
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task ReportMerger_EmptyInputReturnsEmptySummary()
    {
        // Property: Merging empty list produces zero counts
        var result = ReportMerger.Merge([], "test");

        await Assert.That(result.Summary.Total).IsEqualTo(0);
        await Assert.That(result.Summary.Passed).IsEqualTo(0);
        await Assert.That(result.Summary.Failed).IsEqualTo(0);
        await Assert.That(result.Summary.Pending).IsEqualTo(0);
        await Assert.That(result.Summary.Skipped).IsEqualTo(0);
    }

    [Test]
    public void ReportMerger_SingleReportPreservesValues()
    {
        // Property: Merging a single report preserves all summary values
        Prop.ForAll<int, int, int>((passed, failed, pending) =>
        {
            var p = Math.Abs(passed % 100);
            var f = Math.Abs(failed % 100);
            var pe = Math.Abs(pending % 100);
            var s = Math.Abs((passed + failed) % 100);

            var report = new SpecReport
            {
                Timestamp = DateTime.UtcNow,
                Source = "original",
                Contexts = [],
                Summary = new SpecSummary
                {
                    Total = p + f + pe + s,
                    Passed = p,
                    Failed = f,
                    Pending = pe,
                    Skipped = s,
                    DurationMs = 100
                }
            };

            var json = report.ToJson();
            var merged = ReportMerger.Merge([json], "merged");

            return merged.Summary.Total == report.Summary.Total &&
                   merged.Summary.Passed == report.Summary.Passed &&
                   merged.Summary.Failed == report.Summary.Failed &&
                   merged.Summary.Pending == report.Summary.Pending &&
                   merged.Summary.Skipped == report.Summary.Skipped;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ReportMerger_DurationAdditivity()
    {
        // Property: Merged duration equals sum of individual durations
        Prop.ForAll<int, int>((d1, d2) =>
        {
            var dur1 = Math.Abs(d1 % 10000);
            var dur2 = Math.Abs(d2 % 10000);

            var report1 = CreateReport(1, 0, 0, 0, dur1);
            var report2 = CreateReport(0, 1, 0, 0, dur2);

            var merged = ReportMerger.Merge([report1.ToJson(), report2.ToJson()], "test");

            // Allow small floating point tolerance
            return Math.Abs(merged.Summary.DurationMs - (dur1 + dur2)) < 0.001;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ReportMerger_CountsAreAdditive()
    {
        // Property: Merged counts equal sum of individual counts
        Prop.ForAll<int, int, int>((p1, f1, p2) =>
        {
            var passed1 = Math.Abs(p1 % 50);
            var failed1 = Math.Abs(f1 % 50);
            var passed2 = Math.Abs(p2 % 50);
            var failed2 = Math.Abs((p1 + f1) % 50);

            var report1 = CreateReport(passed1, failed1, 0, 0, 100);
            var report2 = CreateReport(passed2, failed2, 0, 0, 100);

            var merged = ReportMerger.Merge([report1.ToJson(), report2.ToJson()], "test");

            return merged.Summary.Passed == passed1 + passed2 &&
                   merged.Summary.Failed == failed1 + failed2 &&
                   merged.Summary.Total == (passed1 + failed1 + passed2 + failed2);
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ReportMerger_MergeIsCommutativeForCounts()
    {
        // Property: Order of merging doesn't affect final counts
        Prop.ForAll<int, int, int>((p1, f1, p2) =>
        {
            var passed1 = Math.Abs(p1 % 50);
            var failed1 = Math.Abs(f1 % 50);
            var passed2 = Math.Abs(p2 % 50);
            var failed2 = Math.Abs((p1 + f1) % 50);

            var report1 = CreateReport(passed1, failed1, 0, 0, 100);
            var report2 = CreateReport(passed2, failed2, 0, 0, 200);

            var merged12 = ReportMerger.Merge([report1.ToJson(), report2.ToJson()], "test");
            var merged21 = ReportMerger.Merge([report2.ToJson(), report1.ToJson()], "test");

            return merged12.Summary.Passed == merged21.Summary.Passed &&
                   merged12.Summary.Failed == merged21.Summary.Failed &&
                   merged12.Summary.Total == merged21.Summary.Total;
        }).QuickCheckThrowOnFailure();
    }

    private static SpecReport CreateReport(int passed, int failed, int pending, int skipped, double duration)
    {
        return new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Source = "test",
            Contexts = [],
            Summary = new SpecSummary
            {
                Total = passed + failed + pending + skipped,
                Passed = passed,
                Failed = failed,
                Pending = pending,
                Skipped = skipped,
                DurationMs = duration
            }
        };
    }
}

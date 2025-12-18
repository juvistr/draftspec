using DraftSpec.Formatters;
using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for SpecDiffService.
/// </summary>
public class SpecDiffServiceTests
{
    #region No Changes

    [Test]
    public async Task Compare_BothNull_ReturnsEmptyDiff()
    {
        var diff = SpecDiffService.Compare(null, null);

        await Assert.That(diff.HasRegressions).IsFalse();
        await Assert.That(diff.Changes).IsEmpty();
    }

    [Test]
    public async Task Compare_IdenticalResults_NoChanges()
    {
        var baseline = CreateReport(("Test > spec1", "passed"), ("Test > spec2", "passed"));
        var current = CreateReport(("Test > spec1", "passed"), ("Test > spec2", "passed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.HasRegressions).IsFalse();
        await Assert.That(diff.NewPassing).IsEqualTo(0);
        await Assert.That(diff.NewFailing).IsEqualTo(0);
        await Assert.That(diff.StillPassing).IsEqualTo(2);
        await Assert.That(diff.Changes).IsEmpty();
    }

    #endregion

    #region Regressions

    [Test]
    public async Task Compare_Regression_Detected()
    {
        var baseline = CreateReport(("Test > spec1", "passed"));
        var current = CreateReport(("Test > spec1", "failed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.HasRegressions).IsTrue();
        await Assert.That(diff.NewFailing).IsEqualTo(1);
        await Assert.That(diff.Changes.Count).IsEqualTo(1);
        await Assert.That(diff.Changes[0].Type).IsEqualTo(ChangeType.Regression);
        await Assert.That(diff.Changes[0].SpecPath).IsEqualTo("Test > spec1");
    }

    [Test]
    public async Task Compare_MultipleRegressions_AllDetected()
    {
        var baseline = CreateReport(
            ("Test > spec1", "passed"),
            ("Test > spec2", "passed"),
            ("Test > spec3", "passed"));
        var current = CreateReport(
            ("Test > spec1", "failed"),
            ("Test > spec2", "passed"),
            ("Test > spec3", "failed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.HasRegressions).IsTrue();
        await Assert.That(diff.NewFailing).IsEqualTo(2);
        await Assert.That(diff.StillPassing).IsEqualTo(1);
    }

    #endregion

    #region Fixes

    [Test]
    public async Task Compare_Fix_Detected()
    {
        var baseline = CreateReport(("Test > spec1", "failed"));
        var current = CreateReport(("Test > spec1", "passed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.HasRegressions).IsFalse();
        await Assert.That(diff.NewPassing).IsEqualTo(1);
        await Assert.That(diff.Changes.Count).IsEqualTo(1);
        await Assert.That(diff.Changes[0].Type).IsEqualTo(ChangeType.Fix);
    }

    [Test]
    public async Task Compare_MixedFixesAndRegressions()
    {
        var baseline = CreateReport(
            ("Test > spec1", "passed"),
            ("Test > spec2", "failed"));
        var current = CreateReport(
            ("Test > spec1", "failed"),
            ("Test > spec2", "passed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.HasRegressions).IsTrue();
        await Assert.That(diff.NewFailing).IsEqualTo(1);
        await Assert.That(diff.NewPassing).IsEqualTo(1);
    }

    #endregion

    #region New and Removed Specs

    [Test]
    public async Task Compare_NewSpec_Detected()
    {
        var baseline = CreateReport(("Test > spec1", "passed"));
        var current = CreateReport(("Test > spec1", "passed"), ("Test > spec2", "passed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.NewSpecs).IsEqualTo(1);
        await Assert.That(diff.Changes.Any(c => c.Type == ChangeType.New)).IsTrue();
    }

    [Test]
    public async Task Compare_RemovedSpec_Detected()
    {
        var baseline = CreateReport(("Test > spec1", "passed"), ("Test > spec2", "passed"));
        var current = CreateReport(("Test > spec1", "passed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.RemovedSpecs).IsEqualTo(1);
        await Assert.That(diff.Changes.Any(c => c.Type == ChangeType.Removed)).IsTrue();
    }

    #endregion

    #region Still Failing

    [Test]
    public async Task Compare_StillFailing_Tracked()
    {
        var baseline = CreateReport(("Test > spec1", "failed"));
        var current = CreateReport(("Test > spec1", "failed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.StillFailing).IsEqualTo(1);
        await Assert.That(diff.HasRegressions).IsFalse(); // Not a new regression
    }

    #endregion

    #region Summary

    [Test]
    public async Task Summary_WithRegressions_ShowsWarning()
    {
        var baseline = CreateReport(("Test > spec1", "passed"));
        var current = CreateReport(("Test > spec1", "failed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.Summary).Contains("regression");
    }

    [Test]
    public async Task Summary_WithFixesOnly_ShowsSuccess()
    {
        var baseline = CreateReport(("Test > spec1", "failed"));
        var current = CreateReport(("Test > spec1", "passed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.Summary).Contains("fix");
        await Assert.That(diff.Summary).Contains("no regression");
    }

    [Test]
    public async Task Summary_NoChanges_ShowsNoChanges()
    {
        var baseline = CreateReport(("Test > spec1", "passed"));
        var current = CreateReport(("Test > spec1", "passed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.Summary).Contains("No changes");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Compare_BaselineNull_AllSpecsAreNew()
    {
        var current = CreateReport(("Test > spec1", "passed"), ("Test > spec2", "failed"));

        var diff = SpecDiffService.Compare(null, current);

        await Assert.That(diff.NewSpecs).IsEqualTo(2);
    }

    [Test]
    public async Task Compare_CurrentNull_AllSpecsRemoved()
    {
        var baseline = CreateReport(("Test > spec1", "passed"), ("Test > spec2", "failed"));

        var diff = SpecDiffService.Compare(baseline, null);

        await Assert.That(diff.RemovedSpecs).IsEqualTo(2);
    }

    [Test]
    public async Task Compare_StatusNormalization_PassAndPassed()
    {
        var baseline = CreateReport(("Test > spec1", "pass"));
        var current = CreateReport(("Test > spec1", "passed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.StillPassing).IsEqualTo(1);
        await Assert.That(diff.Changes).IsEmpty();
    }

    [Test]
    public async Task Compare_StatusNormalization_FailAndFailed()
    {
        var baseline = CreateReport(("Test > spec1", "fail"));
        var current = CreateReport(("Test > spec1", "failed"));

        var diff = SpecDiffService.Compare(baseline, current);

        await Assert.That(diff.StillFailing).IsEqualTo(1);
    }

    #endregion

    #region Helpers

    private static SpecReport CreateReport(params (string path, string status)[] specs)
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary(),
            Contexts = []
        };

        foreach (var (path, status) in specs)
        {
            var parts = path.Split(" > ");
            var contextDesc = parts[0];
            var specDesc = parts.Length > 1 ? string.Join(" > ", parts.Skip(1)) : parts[0];

            var context = report.Contexts.FirstOrDefault(c => c.Description == contextDesc);
            if (context == null)
            {
                context = new SpecContextReport { Description = contextDesc };
                report.Contexts.Add(context);
            }

            // Handle nested paths
            var currentContext = context;
            for (var i = 1; i < parts.Length - 1; i++)
            {
                var nestedDesc = parts[i];
                var nested = currentContext.Contexts.FirstOrDefault(c => c.Description == nestedDesc);
                if (nested == null)
                {
                    nested = new SpecContextReport { Description = nestedDesc };
                    currentContext.Contexts.Add(nested);
                }
                currentContext = nested;
            }

            currentContext.Specs.Add(new SpecResultReport
            {
                Description = parts[^1],
                Status = status
            });
        }

        return report;
    }

    #endregion
}

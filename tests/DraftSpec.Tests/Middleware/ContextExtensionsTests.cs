using DraftSpec.Coverage;
using DraftSpec.Middleware;

namespace DraftSpec.Tests.Middleware;

/// <summary>
/// Tests for type-safe context extension methods.
/// </summary>
public class ContextExtensionsTests
{
    [Test]
    public async Task SetCoverageData_GetCoverageData_RoundTrips()
    {
        var context = CreateContext();
        var coverageData = CreateCoverageData("test-spec");

        context.SetCoverageData(coverageData);
        var retrieved = context.GetCoverageData();

        await Assert.That(retrieved).IsSameReferenceAs(coverageData);
    }

    [Test]
    public async Task GetCoverageData_WhenNotSet_ReturnsNull()
    {
        var context = CreateContext();

        var result = context.GetCoverageData();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task SetCoverageData_OverwritesPreviousValue()
    {
        var context = CreateContext();
        var first = CreateCoverageData("first");
        var second = CreateCoverageData("second");

        context.SetCoverageData(first);
        context.SetCoverageData(second);

        await Assert.That(context.GetCoverageData()).IsSameReferenceAs(second);
    }

    [Test]
    public async Task MultipleContexts_HaveIndependentItems()
    {
        var context1 = CreateContext();
        var context2 = CreateContext();
        var data1 = CreateCoverageData("spec1");
        var data2 = CreateCoverageData("spec2");

        context1.SetCoverageData(data1);
        context2.SetCoverageData(data2);

        await Assert.That(context1.GetCoverageData()!.SpecId).IsEqualTo("spec1");
        await Assert.That(context2.GetCoverageData()!.SpecId).IsEqualTo("spec2");
    }

    private static SpecExecutionContext CreateContext()
    {
        return new SpecExecutionContext
        {
            Spec = new SpecDefinition("test", () => Task.CompletedTask),
            Context = new SpecContext("test"),
            ContextPath = [],
            HasFocused = false
        };
    }

    private static SpecCoverageData CreateCoverageData(string specId)
    {
        return new SpecCoverageData
        {
            SpecId = specId,
            FilesCovered = new Dictionary<string, CoveredFile>(),
            Summary = new SpecCoverageSummary { LinesCovered = 0, FilesTouched = 0 }
        };
    }
}

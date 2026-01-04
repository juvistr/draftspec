using DraftSpec.Internal;

namespace DraftSpec.Tests.Internal;

/// <summary>
/// Tests for SpecExecutor execution helper.
/// </summary>
public class SpecExecutorTests
{
    [Test]
    public async Task Execute_WithPassingSpec_ReturnsPassedStatus()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("passes", () => { }));

        var report = await SpecExecutor.ExecuteAsync(context);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Passed).IsEqualTo(1);
        await Assert.That(report.Summary.Failed).IsEqualTo(0);
    }

    [Test]
    public async Task Execute_WithFailingSpec_ReturnsFailedStatus()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception("failure")));

        var report = await SpecExecutor.ExecuteAsync(context);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Failed).IsEqualTo(1);
        await Assert.That(report.Summary.Passed).IsEqualTo(0);
    }

    [Test]
    public async Task Execute_WithPendingSpec_ReturnsPendingStatus()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("pending"));

        var report = await SpecExecutor.ExecuteAsync(context);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Pending).IsEqualTo(1);
    }

    [Test]
    public async Task Execute_WithSkippedSpec_ReturnsSkippedStatus()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("skipped", () => { }) { IsSkipped = true });

        var report = await SpecExecutor.ExecuteAsync(context);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Skipped).IsEqualTo(1);
    }

    [Test]
    public async Task Execute_WithMultipleSpecs_ReturnsCorrectCounts()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("passes", () => { }));
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception()));
        context.AddSpec(new SpecDefinition("pending"));

        var report = await SpecExecutor.ExecuteAsync(context);

        await Assert.That(report.Summary.Total).IsEqualTo(3);
        await Assert.That(report.Summary.Passed).IsEqualTo(1);
        await Assert.That(report.Summary.Failed).IsEqualTo(1);
        await Assert.That(report.Summary.Pending).IsEqualTo(1);
    }

    [Test]
    public async Task Execute_WithNestedContexts_ReturnsAllSpecs()
    {
        var root = new SpecContext("root");
        root.AddSpec(new SpecDefinition("root spec", () => { }));

        var child = new SpecContext("child", root);
        child.AddSpec(new SpecDefinition("child spec", () => { }));

        var report = await SpecExecutor.ExecuteAsync(root);

        await Assert.That(report.Summary.Total).IsEqualTo(2);
        await Assert.That(report.Summary.Passed).IsEqualTo(2);
    }

    [Test]
    public async Task Execute_ReturnsReportWithTimestamp()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec", () => { }));

        var before = DateTime.UtcNow;
        var report = await SpecExecutor.ExecuteAsync(context);
        var after = DateTime.UtcNow;

        await Assert.That(report.Timestamp).IsGreaterThanOrEqualTo(before);
        await Assert.That(report.Timestamp).IsLessThanOrEqualTo(after);
    }

    [Test]
    public async Task Execute_ReturnsReportWithDuration()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec", () => Thread.Sleep(10)));

        var report = await SpecExecutor.ExecuteAsync(context);

        await Assert.That(report.Summary.DurationMs).IsGreaterThan(0);
    }

    [Test]
    public async Task Execute_ReturnsReportWithContextDescription()
    {
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("adds numbers", () => { }));

        var report = await SpecExecutor.ExecuteAsync(context);

        await Assert.That(report.Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Description).IsEqualTo("Calculator");
    }
}

using System.Text.Json;
using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Internal;
using DraftSpec.Middleware;
using DraftSpec.Plugins;

namespace DraftSpec.Tests.Integration;

/// <summary>
/// Integration tests for end-to-end workflows.
/// </summary>
public class IntegrationTests
{
    #region Full Pipeline Tests

    [Test]
    public async Task FullPipeline_DescribeItExpectRunReport()
    {
        // Arrange - Build a spec tree with nested contexts
        var root = new SpecContext("Calculator");
        var addContext = new SpecContext("add", root);
        addContext.AddSpec(new SpecDefinition("returns sum of two numbers", () =>
        {
            var result = 2 + 3;
            if (result != 5) throw new AssertionException("Expected 5");
        }));
        addContext.AddSpec(new SpecDefinition("handles negative numbers", () =>
        {
            var result = -2 + 3;
            if (result != 1) throw new AssertionException("Expected 1");
        }));

        var subtractContext = new SpecContext("subtract", root);
        subtractContext.AddSpec(new SpecDefinition("returns difference", () =>
        {
            var result = 5 - 3;
            if (result != 2) throw new AssertionException("Expected 2");
        }));

        // Act
        var report = SpecExecutor.Execute(root);

        // Assert
        await Assert.That(report.Summary.Total).IsEqualTo(3);
        await Assert.That(report.Summary.Passed).IsEqualTo(3);
        await Assert.That(report.Summary.Failed).IsEqualTo(0);
        await Assert.That(report.Contexts).Count().IsEqualTo(1);
        await Assert.That(report.Contexts[0].Description).IsEqualTo("Calculator");
        await Assert.That(report.Contexts[0].Contexts).Count().IsEqualTo(2);
    }

    [Test]
    public async Task FullPipeline_WithMiddleware_RetryAndTimeout()
    {
        var context = new SpecContext("flaky tests");
        var attempts = 0;
        context.AddSpec(new SpecDefinition("eventually passes", () =>
        {
            attempts++;
            if (attempts < 3) throw new Exception("Still failing");
        }));

        var runner = new SpecRunnerBuilder()
            .WithRetry(3)
            .WithTimeout(1000)
            .Build();

        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(attempts).IsEqualTo(3);
    }

    [Test]
    public async Task FullPipeline_WithReporters_ReceivesCallbacks()
    {
        var specCompletedCalls = new List<SpecResult>();
        var mockReporter = new MockReporter(specCompletedCalls);

        var configuration = new DraftSpecConfiguration();
        configuration.AddReporter(mockReporter);

        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));

        var runner = new SpecRunnerBuilder().WithConfiguration(configuration).Build();
        runner.Run(context);

        await Assert.That(specCompletedCalls).Count().IsEqualTo(2);
        await Assert.That(specCompletedCalls[0].Spec.Description).IsEqualTo("spec1");
        await Assert.That(specCompletedCalls[1].Spec.Description).IsEqualTo("spec2");
    }

    [Test]
    public async Task FullPipeline_WithFilterMiddleware()
    {
        var context = new SpecContext("test suite");
        context.AddSpec(new SpecDefinition("tagged spec", () => { }) { Tags = ["integration"] });
        context.AddSpec(new SpecDefinition("untagged spec", () => { }));

        // Filter to only run specs with "integration" tag
        var runner = new SpecRunnerBuilder()
            .WithFilter(ctx => ctx.Spec.Tags.Contains("integration"))
            .Build();

        var results = runner.Run(context);

        await Assert.That(results).Count().IsEqualTo(2);
        var passedCount = results.Count(r => r.Status == SpecStatus.Passed);
        var skippedCount = results.Count(r => r.Status == SpecStatus.Skipped);
        await Assert.That(passedCount).IsEqualTo(1);
        await Assert.That(skippedCount).IsEqualTo(1);
    }

    #endregion

    #region Output Format Tests

    [Test]
    public async Task JsonFormatter_ProducesValidJson()
    {
        var context = new SpecContext("JSON test");
        context.AddSpec(new SpecDefinition("passes", () => { }));
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception("error")));

        var report = SpecExecutor.Execute(context);
        var formatter = new JsonFormatter();
        var json = formatter.Format(report);

        // Verify JSON is valid
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        await Assert.That(root.GetProperty("summary").GetProperty("total").GetInt32()).IsEqualTo(2);
        await Assert.That(root.GetProperty("summary").GetProperty("passed").GetInt32()).IsEqualTo(1);
        await Assert.That(root.GetProperty("summary").GetProperty("failed").GetInt32()).IsEqualTo(1);
    }

    [Test]
    public async Task Report_JsonRoundtrip_WorksCorrectly()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("passes", () => { }));
        context.AddSpec(new SpecDefinition("pending"));

        var report = SpecExecutor.Execute(context);
        var formatter = new JsonFormatter();
        var json = formatter.Format(report);

        // Verify JSON can be deserialized back
        var deserialized = SpecReport.FromJson(json);

        await Assert.That(deserialized.Summary.Total).IsEqualTo(2);
    }

    #endregion

    #region SpecExecutor End-to-End Tests

    [Test]
    public async Task SpecExecutor_ProducesCompleteReport()
    {
        var root = new SpecContext("Feature");
        var child = new SpecContext("Scenario", root);
        child.AddSpec(new SpecDefinition("step 1", () => { }));
        child.AddSpec(new SpecDefinition("step 2", () => throw new Exception("failed")));
        child.AddSpec(new SpecDefinition("step 3"));

        var report = SpecExecutor.Execute(root);

        await Assert.That(report.Summary.Total).IsEqualTo(3);
        await Assert.That(report.Summary.Passed).IsEqualTo(1);
        await Assert.That(report.Summary.Failed).IsEqualTo(1);
        await Assert.That(report.Summary.Pending).IsEqualTo(1);
        await Assert.That(report.Timestamp).IsNotDefault();
    }

    [Test]
    public async Task SpecExecutor_ReportCanBeSerializedToJson()
    {
        var context = new SpecContext("Serialization test");
        context.AddSpec(new SpecDefinition("test", () => { }));

        var report = SpecExecutor.Execute(context);
        var formatter = new JsonFormatter();
        var json = formatter.Format(report);
        var deserializedReport = SpecReport.FromJson(json);

        await Assert.That(deserializedReport.Summary.Total).IsEqualTo(1);
        await Assert.That(deserializedReport.Summary.Passed).IsEqualTo(1);
        await Assert.That(deserializedReport.Contexts[0].Description).IsEqualTo("Serialization test");
    }

    [Test]
    public async Task SpecExecutor_ExitCodeLogic()
    {
        // Test with passing specs
        var passingContext = new SpecContext("passing");
        passingContext.AddSpec(new SpecDefinition("passes", () => { }));
        var passingReport = SpecExecutor.Execute(passingContext);
        var passingExitCode = passingReport.Summary.Failed > 0 ? 1 : 0;

        // Test with failing specs
        var failingContext = new SpecContext("failing");
        failingContext.AddSpec(new SpecDefinition("fails", () => throw new Exception()));
        var failingReport = SpecExecutor.Execute(failingContext);
        var failingExitCode = failingReport.Summary.Failed > 0 ? 1 : 0;

        await Assert.That(passingExitCode).IsEqualTo(0);
        await Assert.That(failingExitCode).IsEqualTo(1);
    }

    #endregion

    #region Test Helpers

    private class MockReporter : IReporter
    {
        private readonly List<SpecResult> _completedSpecs;

        public string Name => "MockReporter";

        public MockReporter(List<SpecResult> completedSpecs)
        {
            _completedSpecs = completedSpecs;
        }

        public Task OnRunStartingAsync(RunStartingContext context) => Task.CompletedTask;

        public Task OnSpecCompletedAsync(SpecResult result)
        {
            _completedSpecs.Add(result);
            return Task.CompletedTask;
        }

        public Task OnRunCompletedAsync(SpecReport report) => Task.CompletedTask;
    }

    #endregion
}

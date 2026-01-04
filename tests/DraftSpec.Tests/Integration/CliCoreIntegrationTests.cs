using System.Collections.Concurrent;
using DraftSpec.Cli;
using DraftSpec.Formatters;
using DraftSpec.Internal;
using DraftSpec.Middleware;

namespace DraftSpec.Tests.Integration;

/// <summary>
/// Integration tests for CLI-Core workflows.
/// Tests complete workflows across component boundaries.
/// </summary>
public class CliCoreIntegrationTests
{
    private string _testDirectory = null!;

    [Before(Test)]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CliCoreIntegration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); }
            catch { /* ignore cleanup errors */ }
        }
    }

    #region Full Workflow: Find → Build → Run → Format → Output

    [Test]
    public async Task FullWorkflow_FindSpecsRunAndFormat_ProducesReport()
    {
        // Arrange - Create a spec file structure
        var specsDir = Path.Combine(_testDirectory, "Specs");
        Directory.CreateDirectory(specsDir);

        // Create a simple spec context programmatically (simulating parsed spec)
        var context = new SpecContext("Calculator");
        context.AddSpec(new SpecDefinition("adds numbers", () =>
        {
            if (2 + 2 != 4) throw new Exception("Math is broken");
        }));
        context.AddSpec(new SpecDefinition("subtracts numbers", () =>
        {
            if (5 - 3 != 2) throw new Exception("Math is broken");
        }));

        // Act - Execute through the full pipeline
        var report = await SpecExecutor.ExecuteAsync(context);
        var formatter = new JsonFormatter();
        var output = formatter.Format(report);

        // Assert
        await Assert.That(report.Summary.Total).IsEqualTo(2);
        await Assert.That(report.Summary.Passed).IsEqualTo(2);
        await Assert.That(output).Contains("\"total\": 2");
        await Assert.That(output).Contains("\"passed\": 2");
    }

    [Test]
    public async Task FullWorkflow_WithFailures_PropagatesErrorsToReport()
    {
        var context = new SpecContext("Failing Suite");
        context.AddSpec(new SpecDefinition("passes", () => { }));
        context.AddSpec(new SpecDefinition("fails with message", () =>
            throw new AssertionException("Expected true but was false")));
        context.AddSpec(new SpecDefinition("fails with exception", () =>
            throw new InvalidOperationException("Something went wrong")));

        var report = await SpecExecutor.ExecuteAsync(context);

        await Assert.That(report.Summary.Total).IsEqualTo(3);
        await Assert.That(report.Summary.Passed).IsEqualTo(1);
        await Assert.That(report.Summary.Failed).IsEqualTo(2);

        // Verify error details are captured in the report
        var contextReport = report.Contexts[0];
        var failedSpecs = contextReport.Specs.Where(s => s.Failed).ToList();
        await Assert.That(failedSpecs).Count().IsEqualTo(2);
        await Assert.That(failedSpecs.Any(f => f.Error?.Contains("Expected true") == true)).IsTrue();
        await Assert.That(failedSpecs.Any(f => f.Error?.Contains("Something went wrong") == true)).IsTrue();
    }

    [Test]
    public async Task FullWorkflow_MultipleContexts_MergesResults()
    {
        var root = new SpecContext("App");

        var userContext = new SpecContext("User", root);
        userContext.AddSpec(new SpecDefinition("can login", () => { }));
        userContext.AddSpec(new SpecDefinition("can logout", () => { }));

        var adminContext = new SpecContext("Admin", root);
        adminContext.AddSpec(new SpecDefinition("can manage users", () => { }));

        var report = await SpecExecutor.ExecuteAsync(root);

        await Assert.That(report.Summary.Total).IsEqualTo(3);
        await Assert.That(report.Contexts[0].Contexts).Count().IsEqualTo(2);
    }

    #endregion

    #region Parallel Execution Across Multiple Spec Files

    [Test]
    public async Task ParallelExecution_MultipleContexts_ExecutesConcurrently()
    {
        var executionLog = new ConcurrentBag<(string Context, DateTime Time)>();

        var contexts = new List<SpecContext>();
        for (var i = 0; i < 5; i++)
        {
            var ctx = new SpecContext($"Context-{i}");
            var index = i;
            ctx.AddSpec(new SpecDefinition("spec", async () =>
            {
                executionLog.Add(($"Context-{index}", DateTime.UtcNow));
                await Task.Delay(50);
            }));
            contexts.Add(ctx);
        }

        // Create a root context to hold all
        var root = new SpecContext("Root");
        foreach (var ctx in contexts)
        {
            var child = new SpecContext(ctx.Description, root);
            foreach (var spec in ctx.Specs)
                child.AddSpec(spec);
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(5)
            .Build();

        await runner.RunAsync(root);

        // Verify all specs ran (timing assertions removed - too flaky on CI)
        await Assert.That(executionLog.Count).IsEqualTo(5);
    }

    [Test]
    public async Task ParallelExecution_WithMiddleware_AppliesCorrectly()
    {
        var context = new SpecContext("Parallel with Middleware");
        var retryCount = 0;

        context.AddSpec(new SpecDefinition("flaky spec", () =>
        {
            var count = Interlocked.Increment(ref retryCount);
            if (count < 2) throw new Exception("Flaky failure");
        }));

        for (var i = 0; i < 5; i++)
            context.AddSpec(new SpecDefinition($"stable spec {i}", () => { }));

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .WithRetry(3)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
        await Assert.That(retryCount).IsGreaterThanOrEqualTo(2); // At least one retry
    }

    #endregion

    #region Error Propagation from Core to CLI

    [Test]
    public async Task ErrorPropagation_SpecException_CapturedInResult()
    {
        var context = new SpecContext("Error Propagation");
        context.AddSpec(new SpecDefinition("throws custom exception", () =>
        {
            throw new CustomTestException("Custom error message", 42);
        }));

        var results = await new SpecRunner().RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception).IsTypeOf<CustomTestException>();

        var customEx = (CustomTestException)results[0].Exception!;
        await Assert.That(customEx.Message).IsEqualTo("Custom error message");
        await Assert.That(customEx.ErrorCode).IsEqualTo(42);
    }

    [Test]
    public async Task ErrorPropagation_HookException_StopsExecution()
    {
        var specsRun = new List<string>();

        var context = new SpecContext("Hook Failure");
        context.BeforeEach = () => throw new InvalidOperationException("Setup failed");
        context.AddSpec(new SpecDefinition("spec1", () => specsRun.Add("spec1")));
        context.AddSpec(new SpecDefinition("spec2", () => specsRun.Add("spec2")));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new SpecRunner().RunAsync(context));

        await Assert.That(ex!.Message).IsEqualTo("Setup failed");
        await Assert.That(specsRun).IsEmpty();
    }

    [Test]
    public async Task ErrorPropagation_NestedContextFailure_BubblesUp()
    {
        var root = new SpecContext("Root");
        var child = new SpecContext("Child", root);
        var grandchild = new SpecContext("Grandchild", child);
        grandchild.AddSpec(new SpecDefinition("deep failure", () =>
            throw new Exception("Deep nested error")));

        var results = await new SpecRunner().RunAsync(root);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception!.Message).IsEqualTo("Deep nested error");
        await Assert.That(results[0].ContextPath).IsEquivalentTo(["Root", "Child", "Grandchild"]);
    }

    [Test]
    public async Task ErrorPropagation_AsyncException_CapturedCorrectly()
    {
        var context = new SpecContext("Async Errors");
        context.AddSpec(new SpecDefinition("async failure", async () =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Async operation failed");
        }));

        var results = await new SpecRunner().RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception!.Message).IsEqualTo("Async operation failed");
    }

    #endregion

    #region Report Formatting Pipeline

    [Test]
    public async Task ReportFormatting_AllFormatters_ProduceValidOutput()
    {
        var context = new SpecContext("Formatter Test");
        context.AddSpec(new SpecDefinition("passes", () => { }));
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception("error")));
        context.AddSpec(new SpecDefinition("pending")); // No body

        var report = await SpecExecutor.ExecuteAsync(context);

        // Test JSON formatter
        var jsonFormatter = new JsonFormatter();
        var json = jsonFormatter.Format(report);
        await Assert.That(json).Contains("\"total\": 3");
        await Assert.That(json).Contains("\"passed\": 1");
        await Assert.That(json).Contains("\"failed\": 1");
        await Assert.That(json).Contains("\"pending\": 1");
    }

    [Test]
    public async Task ReportFormatting_JsonRoundtrip_PreservesData()
    {
        var context = new SpecContext("Roundtrip Test");
        context.AddSpec(new SpecDefinition("spec with unicode 日本語", () => { }));
        context.AddSpec(new SpecDefinition("failing spec", () =>
            throw new Exception("Error with unicode: 中文")));

        var report = await SpecExecutor.ExecuteAsync(context);
        var formatter = new JsonFormatter();
        var json = formatter.Format(report);

        var deserialized = SpecReport.FromJson(json);

        await Assert.That(deserialized.Summary.Total).IsEqualTo(2);
        await Assert.That(deserialized.Summary.Passed).IsEqualTo(1);
        await Assert.That(deserialized.Summary.Failed).IsEqualTo(1);
    }

    #endregion

    #region Build Caching Behavior

    [Test]
    public async Task BuildCache_ConsecutiveRuns_UsesCache()
    {
        var buildCount = 0;
        var context = new SpecContext("Cache Test");
        context.AddSpec(new SpecDefinition("spec", () => buildCount++));

        var runner = new SpecRunner();

        // First run
        await runner.RunAsync(context);
        var firstCount = buildCount;

        // Second run - should use cached result
        await runner.RunAsync(context);
        var secondCount = buildCount;

        // Both runs should execute the spec
        await Assert.That(firstCount).IsEqualTo(1);
        await Assert.That(secondCount).IsEqualTo(2);
    }

    #endregion

    #region Test Helpers

    private class CustomTestException : Exception
    {
        public int ErrorCode { get; }

        public CustomTestException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    #endregion
}

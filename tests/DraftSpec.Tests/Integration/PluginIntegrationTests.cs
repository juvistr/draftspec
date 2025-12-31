using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Internal;
using DraftSpec.Middleware;
using DraftSpec.Plugins;

namespace DraftSpec.Tests.Integration;

/// <summary>
/// Integration tests for plugin system workflows.
/// Tests formatter, reporter, and middleware plugin lifecycles.
/// </summary>
public class PluginIntegrationTests
{
    #region Custom Formatter Integration

    [Test]
    public async Task CustomFormatter_Registration_UsedForOutput()
    {
        var context = new SpecContext("Formatter Test");
        context.AddSpec(new SpecDefinition("passes", () => { }));
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception("error")));

        var report = SpecExecutor.Execute(context);

        var formatter = new CustomXmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).StartsWith("<?xml");
        await Assert.That(output).Contains("<passed>1</passed>");
        await Assert.That(output).Contains("<failed>1</failed>");
    }

    [Test]
    public async Task CustomFormatter_WithOptions_AppliesConfiguration()
    {
        var context = new SpecContext("Options Test");
        context.AddSpec(new SpecDefinition("spec", () => { }));

        var report = SpecExecutor.Execute(context);

        var formatter = new CustomXmlFormatter { IncludeTimestamp = false };
        var output = formatter.Format(report);

        await Assert.That(output).DoesNotContain("<timestamp>");
    }

    #endregion

    #region Custom Reporter Lifecycle

    [Test]
    public async Task CustomReporter_ReceivesLifecycleEvents()
    {
        var reporter = new LifecycleTrackingReporter();
        var configuration = new DraftSpecConfiguration();
        configuration.AddReporter(reporter);

        var context = new SpecContext("Lifecycle Test");
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));
        context.AddSpec(new SpecDefinition("spec3", () => throw new Exception("fail")));

        var runner = new SpecRunnerBuilder().WithConfiguration(configuration).Build();
        await runner.RunAsync(context);

        // Verify run starting and spec completion events are received
        await Assert.That(reporter.RunStartedCalled).IsTrue();
        await Assert.That(reporter.SpecCompletedCount).IsEqualTo(3);
    }

    [Test]
    public async Task CustomReporter_StreamingEvents_ReceivedInOrder()
    {
        var reporter = new OrderTrackingReporter();
        var configuration = new DraftSpecConfiguration();
        configuration.AddReporter(reporter);

        var context = new SpecContext("Order Test");
        context.AddSpec(new SpecDefinition("first", () => { }));
        context.AddSpec(new SpecDefinition("second", () => { }));
        context.AddSpec(new SpecDefinition("third", () => { }));

        var runner = new SpecRunnerBuilder().WithConfiguration(configuration).Build();
        await runner.RunAsync(context);

        // Verify events received in order: RunStarting, then specs in order
        await Assert.That(reporter.EventOrder).Count().IsGreaterThanOrEqualTo(4);
        await Assert.That(reporter.EventOrder[0]).IsEqualTo("RunStarting");
        await Assert.That(reporter.EventOrder[1]).IsEqualTo("SpecCompleted:first");
        await Assert.That(reporter.EventOrder[2]).IsEqualTo("SpecCompleted:second");
        await Assert.That(reporter.EventOrder[3]).IsEqualTo("SpecCompleted:third");
    }

    [Test]
    public async Task MultipleReporters_AllReceiveEvents()
    {
        var reporter1 = new LifecycleTrackingReporter();
        var reporter2 = new LifecycleTrackingReporter();
        var configuration = new DraftSpecConfiguration();
        configuration.AddReporter(reporter1);
        configuration.AddReporter(reporter2);

        var context = new SpecContext("Multi Reporter Test");
        context.AddSpec(new SpecDefinition("spec", () => { }));

        var runner = new SpecRunnerBuilder().WithConfiguration(configuration).Build();
        await runner.RunAsync(context);

        // Both reporters should receive run starting and spec completion events
        await Assert.That(reporter1.RunStartedCalled).IsTrue();
        await Assert.That(reporter2.RunStartedCalled).IsTrue();
        await Assert.That(reporter1.SpecCompletedCount).IsEqualTo(1);
        await Assert.That(reporter2.SpecCompletedCount).IsEqualTo(1);
    }

    #endregion

    #region Middleware Plugin Registration and Ordering

    [Test]
    public async Task MiddlewareChain_ExecutesInOrder()
    {
        var executionOrder = new List<string>();

        var context = new SpecContext("Middleware Order");
        context.AddSpec(new SpecDefinition("spec", () => executionOrder.Add("spec")));

        var runner = SpecRunner.Create()
            .Use(new OrderTrackingMiddleware("first", executionOrder))
            .Use(new OrderTrackingMiddleware("second", executionOrder))
            .Use(new OrderTrackingMiddleware("third", executionOrder))
            .Build();

        await runner.RunAsync(context);

        // Middleware wraps: first wraps second wraps third wraps spec
        // Execution order: first-before, second-before, third-before, spec, third-after, second-after, first-after
        await Assert.That(executionOrder).Contains("first-before");
        await Assert.That(executionOrder).Contains("second-before");
        await Assert.That(executionOrder).Contains("third-before");
        await Assert.That(executionOrder).Contains("spec");
        await Assert.That(executionOrder.IndexOf("first-before")).IsLessThan(executionOrder.IndexOf("second-before"));
        await Assert.That(executionOrder.IndexOf("second-before")).IsLessThan(executionOrder.IndexOf("spec"));
    }

    [Test]
    public async Task MiddlewareChain_RetryAndTimeout_WorkTogether()
    {
        var attempts = 0;
        var context = new SpecContext("Combined Middleware");
        context.AddSpec(new SpecDefinition("flaky with timeout", () =>
        {
            attempts++;
            if (attempts < 3) throw new Exception("Still failing");
        }));

        var runner = SpecRunner.Create()
            .WithTimeout(5000)
            .WithRetry(5)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(attempts).IsEqualTo(3);
    }

    [Test]
    public async Task MiddlewareChain_FilterShortCircuits()
    {
        var executedSpecs = new List<string>();

        var context = new SpecContext("Filter Test");
        context.AddSpec(new SpecDefinition("included", () => executedSpecs.Add("included")) { Tags = ["run"] });
        context.AddSpec(new SpecDefinition("excluded", () => executedSpecs.Add("excluded")));

        var runner = SpecRunner.Create()
            .WithFilter(ctx => ctx.Spec.Tags.Contains("run"))
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(executedSpecs).Contains("included");
        await Assert.That(executedSpecs).DoesNotContain("excluded");
        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(1);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Skipped)).IsEqualTo(1);
    }

    [Test]
    public async Task CustomMiddleware_CanModifyResult()
    {
        var context = new SpecContext("Result Modifier");
        context.AddSpec(new SpecDefinition("spec", () => throw new Exception("Original error")));

        var runner = SpecRunner.Create()
            .Use(new ResultModifyingMiddleware())
            .Build();

        var results = await runner.RunAsync(context);

        // The middleware should have changed the result
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    #region Plugin Context and State Sharing

    [Test]
    public async Task PluginContext_SharedAcrossSpecs()
    {
        var sharedState = new SharedState();
        var context = new SpecContext("Shared State");

        context.BeforeEach = () =>
        {
            sharedState.Counter++;
            return Task.CompletedTask;
        };

        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));
        context.AddSpec(new SpecDefinition("spec3", () => { }));

        await new SpecRunner().RunAsync(context);

        await Assert.That(sharedState.Counter).IsEqualTo(3);
    }

    #endregion

    #region Test Helpers

    private class CustomXmlFormatter : IFormatter
    {
        public string FileExtension => ".xml";
        public bool IncludeTimestamp { get; set; } = true;

        public string Format(SpecReport report)
        {
            var xml = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <report>
                  <summary>
                    <total>{report.Summary.Total}</total>
                    <passed>{report.Summary.Passed}</passed>
                    <failed>{report.Summary.Failed}</failed>
                  </summary>
                """;

            if (IncludeTimestamp)
            {
                xml += $"\n  <timestamp>{report.Timestamp:o}</timestamp>";
            }

            xml += "\n</report>";
            return xml;
        }
    }

    private class LifecycleTrackingReporter : IReporter
    {
        public string Name => "LifecycleTracker";
        public bool RunStartedCalled { get; private set; }
        public int SpecCompletedCount { get; private set; }
        public bool RunCompletedCalled { get; private set; }
        public SpecReport? FinalReport { get; private set; }

        public Task OnRunStartingAsync(RunStartingContext context)
        {
            RunStartedCalled = true;
            return Task.CompletedTask;
        }

        public Task OnSpecCompletedAsync(SpecResult result)
        {
            SpecCompletedCount++;
            return Task.CompletedTask;
        }

        public Task OnRunCompletedAsync(SpecReport report)
        {
            RunCompletedCalled = true;
            FinalReport = report;
            return Task.CompletedTask;
        }
    }

    private class OrderTrackingReporter : IReporter
    {
        public string Name => "OrderTracker";
        public List<string> EventOrder { get; } = [];

        public Task OnRunStartingAsync(RunStartingContext context)
        {
            EventOrder.Add("RunStarting");
            return Task.CompletedTask;
        }

        public Task OnSpecCompletedAsync(SpecResult result)
        {
            EventOrder.Add($"SpecCompleted:{result.Spec.Description}");
            return Task.CompletedTask;
        }

        public Task OnRunCompletedAsync(SpecReport report)
        {
            EventOrder.Add("RunCompleted");
            return Task.CompletedTask;
        }
    }

    private class OrderTrackingMiddleware : ISpecMiddleware
    {
        private readonly string _name;
        private readonly List<string> _log;

        public OrderTrackingMiddleware(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context, Func<SpecExecutionContext, Task<SpecResult>> pipeline)
        {
            _log.Add($"{_name}-before");
            var result = await pipeline(context);
            _log.Add($"{_name}-after");
            return result;
        }
    }

    private class ResultModifyingMiddleware : ISpecMiddleware
    {
        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context, Func<SpecExecutionContext, Task<SpecResult>> pipeline)
        {
            var result = await pipeline(context);

            // Convert failures to passes (for testing purposes)
            if (result.Status == SpecStatus.Failed)
            {
                return new SpecResult(
                    result.Spec,
                    SpecStatus.Passed,
                    result.ContextPath,
                    result.Duration,
                    null);
            }

            return result;
        }
    }

    private class SharedState
    {
        public int Counter { get; set; }
    }

    #endregion
}

using DraftSpec.Execution;

namespace DraftSpec.Tests.Execution;

/// <summary>
/// Tests for SequentialExecutionStrategy.
/// </summary>
public class SequentialExecutionStrategyTests
{
    [Test]
    public async Task ExecuteAsync_ExecutesSpecsInOrder()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("spec1", () => { });
        var spec2 = new SpecDefinition("spec2", () => { });
        var spec3 = new SpecDefinition("spec3", () => { });
        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);

        var executionOrder = new List<string>();
        var results = new List<SpecResult>();
        var notifiedResults = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: (spec, ctx, path, focused) =>
            {
                executionOrder.Add(spec.Description);
                return Task.FromResult(new SpecResult(spec, SpecStatus.Passed, path));
            },
            notifyCompleted: result =>
            {
                notifiedResults.Add(result);
                return Task.CompletedTask;
            });

        var strategy = new SequentialExecutionStrategy();
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(executionOrder).Count().IsEqualTo(3);
        await Assert.That(executionOrder[0]).IsEqualTo("spec1");
        await Assert.That(executionOrder[1]).IsEqualTo("spec2");
        await Assert.That(executionOrder[2]).IsEqualTo("spec3");

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(notifiedResults).Count().IsEqualTo(3);
    }

    [Test]
    public async Task ExecuteAsync_WithBailEnabled_StopsAfterFirstFailure()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("spec1", () => { });
        var spec2 = new SpecDefinition("spec2", () => throw new Exception("fail"));
        var spec3 = new SpecDefinition("spec3", () => { });
        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);

        var executionOrder = new List<string>();
        var results = new List<SpecResult>();
        var bailTriggered = false;

        var strategyContext = CreateContext(
            context,
            results,
            bailEnabled: true,
            isBailTriggered: () => bailTriggered,
            signalBail: () => bailTriggered = true,
            runSpec: (spec, ctx, path, focused) =>
            {
                executionOrder.Add(spec.Description);
                if (spec.Description == "spec2")
                    return Task.FromResult(new SpecResult(spec, SpecStatus.Failed, path));
                return Task.FromResult(new SpecResult(spec, SpecStatus.Passed, path));
            });

        var strategy = new SequentialExecutionStrategy();
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        // spec1 and spec2 should have executed, spec3 should be skipped
        await Assert.That(executionOrder).Count().IsEqualTo(2);
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[2].Status).IsEqualTo(SpecStatus.Skipped);
        await Assert.That(bailTriggered).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_WithBailDisabled_ContinuesAfterFailure()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("spec1", () => { });
        var spec2 = new SpecDefinition("spec2", () => throw new Exception("fail"));
        var spec3 = new SpecDefinition("spec3", () => { });
        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);

        var executionOrder = new List<string>();
        var results = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            bailEnabled: false,
            runSpec: (spec, ctx, path, focused) =>
            {
                executionOrder.Add(spec.Description);
                if (spec.Description == "spec2")
                    return Task.FromResult(new SpecResult(spec, SpecStatus.Failed, path));
                return Task.FromResult(new SpecResult(spec, SpecStatus.Passed, path));
            });

        var strategy = new SequentialExecutionStrategy();
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(executionOrder).Count().IsEqualTo(3);
        await Assert.That(results).Count().IsEqualTo(3);
    }

    [Test]
    public async Task ExecuteAsync_RespectsCancellation()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("spec1", () => { });
        var spec2 = new SpecDefinition("spec2", () => { });
        context.AddSpec(spec1);
        context.AddSpec(spec2);

        var cts = new CancellationTokenSource();
        var results = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: (spec, ctx, path, focused) =>
            {
                if (spec.Description == "spec1")
                    cts.Cancel(); // Cancel after first spec
                return Task.FromResult(new SpecResult(spec, SpecStatus.Passed, path));
            });

        var strategy = new SequentialExecutionStrategy();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await strategy.ExecuteAsync(strategyContext, cts.Token));
    }

    [Test]
    public async Task Instance_ReturnsSingleton()
    {
        var instance1 = SequentialExecutionStrategy.Instance;
        var instance2 = SequentialExecutionStrategy.Instance;

        await Assert.That(ReferenceEquals(instance1, instance2)).IsTrue();
    }

    private static SpecExecutionStrategyContext CreateContext(
        SpecContext context,
        List<SpecResult> results,
        bool bailEnabled = false,
        Func<bool>? isBailTriggered = null,
        Action? signalBail = null,
        Func<SpecDefinition, SpecContext, IReadOnlyList<string>, bool, Task<SpecResult>>? runSpec = null,
        Func<SpecResult, Task>? notifyCompleted = null,
        Func<IReadOnlyList<SpecResult>, Task>? notifyBatchCompleted = null)
    {
        return new SpecExecutionStrategyContext
        {
            Specs = context.Specs,
            Context = context,
            ContextPath = Array.Empty<string>(),
            Results = results,
            HasFocused = false,
            BailEnabled = bailEnabled,
            IsBailTriggered = isBailTriggered ?? (() => false),
            SignalBail = signalBail ?? (() => { }),
            RunSpec = runSpec ?? ((spec, ctx, path, focused) =>
                Task.FromResult(new SpecResult(spec, SpecStatus.Passed, path))),
            NotifyCompleted = notifyCompleted ?? (_ => Task.CompletedTask),
            NotifyBatchCompleted = notifyBatchCompleted ?? (_ => Task.CompletedTask)
        };
    }
}

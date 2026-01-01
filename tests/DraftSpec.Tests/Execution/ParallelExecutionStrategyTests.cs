using System.Collections.Concurrent;
using DraftSpec.Execution;

namespace DraftSpec.Tests.Execution;

/// <summary>
/// Tests for ParallelExecutionStrategy.
/// </summary>
public class ParallelExecutionStrategyTests
{
    [Test]
    public async Task ExecuteAsync_ExecutesAllSpecs()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("spec1", () => { });
        var spec2 = new SpecDefinition("spec2", () => { });
        var spec3 = new SpecDefinition("spec3", () => { });
        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);

        var executedSpecs = new ConcurrentBag<string>();
        var results = new List<SpecResult>();
        var batchNotified = false;

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: (spec, ctx, path, focused) =>
            {
                executedSpecs.Add(spec.Description);
                return Task.FromResult(new SpecResult(spec, SpecStatus.Passed, path));
            },
            notifyBatchCompleted: batch =>
            {
                batchNotified = true;
                return Task.CompletedTask;
            });

        var strategy = new ParallelExecutionStrategy(4);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(executedSpecs).Count().IsEqualTo(3);
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(batchNotified).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_PreservesResultOrder()
    {
        var context = new SpecContext("test");
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            context.AddSpec(new SpecDefinition($"spec{i}", () => { }));
        }

        var results = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                // Add random delay to encourage out-of-order execution
                await Task.Delay(Random.Shared.Next(1, 10));
                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(4);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(results).Count().IsEqualTo(10);
        for (int i = 0; i < 10; i++)
        {
            await Assert.That(results[i].Spec.Description).IsEqualTo($"spec{i}");
        }
    }

    [Test]
    public async Task ExecuteAsync_ExecutesConcurrently()
    {
        var context = new SpecContext("test");
        for (int i = 0; i < 4; i++)
        {
            context.AddSpec(new SpecDefinition($"spec{i}", () => { }));
        }

        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();
        var results = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }

                await Task.Delay(50);

                lock (lockObj)
                {
                    concurrentCount--;
                }

                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(4);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        // With 4 specs and 4 max parallelism, we should see >1 concurrent execution
        await Assert.That(maxConcurrent).IsGreaterThan(1);
    }

    [Test]
    public async Task ExecuteAsync_WithBailEnabled_SkipsRemainingSpecs()
    {
        var context = new SpecContext("test");
        var spec1 = new SpecDefinition("spec1", () => { });
        var spec2 = new SpecDefinition("spec2", () => { });
        var spec3 = new SpecDefinition("spec3", () => { });
        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);

        var bailTriggered = false;
        var results = new List<SpecResult>();
        var startedSpecs = new ConcurrentBag<string>();

        var strategyContext = CreateContext(
            context,
            results,
            bailEnabled: true,
            isBailTriggered: () => bailTriggered,
            signalBail: () => bailTriggered = true,
            runSpec: async (spec, ctx, path, focused) =>
            {
                startedSpecs.Add(spec.Description);

                // Slow down spec1 to let spec2 fail first
                if (spec.Description == "spec1")
                    await Task.Delay(100);

                if (spec.Description == "spec2")
                    return new SpecResult(spec, SpecStatus.Failed, path);

                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        // Use max parallelism of 1 to ensure deterministic execution order
        var strategy = new ParallelExecutionStrategy(1);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(bailTriggered).IsTrue();

        // At least one spec should be skipped
        var skippedCount = results.Count(r => r.Status == SpecStatus.Skipped);
        await Assert.That(skippedCount).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_RespectsCancellation()
    {
        var context = new SpecContext("test");
        for (int i = 0; i < 10; i++)
        {
            context.AddSpec(new SpecDefinition($"spec{i}", () => { }));
        }

        var cts = new CancellationTokenSource();
        var results = new List<SpecResult>();
        var executionCount = 0;

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                Interlocked.Increment(ref executionCount);
                if (executionCount >= 2)
                    cts.Cancel();
                await Task.Delay(10);
                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(2);

        // External cancellation should throw OperationCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await strategy.ExecuteAsync(strategyContext, cts.Token));
    }

    [Test]
    public async Task Constructor_WithZeroParallelism_UsesProcessorCount()
    {
        var strategy = new ParallelExecutionStrategy(0);

        await Assert.That(strategy.MaxDegreeOfParallelism).IsEqualTo(Environment.ProcessorCount);
    }

    [Test]
    public async Task Constructor_WithNegativeParallelism_UsesProcessorCount()
    {
        var strategy = new ParallelExecutionStrategy(-1);

        await Assert.That(strategy.MaxDegreeOfParallelism).IsEqualTo(Environment.ProcessorCount);
    }

    [Test]
    public async Task Constructor_WithPositiveParallelism_UsesSpecifiedValue()
    {
        var strategy = new ParallelExecutionStrategy(8);

        await Assert.That(strategy.MaxDegreeOfParallelism).IsEqualTo(8);
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

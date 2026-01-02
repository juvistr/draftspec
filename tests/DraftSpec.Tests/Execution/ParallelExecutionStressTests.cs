using System.Collections.Concurrent;
using DraftSpec.Execution;

namespace DraftSpec.Tests.Execution;

/// <summary>
/// Stress tests for ParallelExecutionStrategy to validate behavior under high concurrency.
/// These tests run multiple iterations to catch intermittent race conditions.
/// </summary>
public class ParallelExecutionStressTests
{
    private const int HighConcurrencySpecCount = 100;
    private const int StressIterations = 10;

    #region High Concurrency Tests

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_100ParallelSpecs_MaintainsResultOrdering()
    {
        var context = new SpecContext("stress-test");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var results = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                // Random delay to encourage out-of-order execution
                await Task.Delay(Random.Shared.Next(1, 5));
                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(results).Count().IsEqualTo(HighConcurrencySpecCount);

        // Verify results maintain original ordering
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            await Assert.That(results[i].Spec.Description).IsEqualTo($"spec-{i}");
        }
    }

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_100ParallelSpecs_NoIndexOutOfBounds()
    {
        var context = new SpecContext("stress-test");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var results = new List<SpecResult>();
        var exceptions = new ConcurrentBag<Exception>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                try
                {
                    await Task.Delay(Random.Shared.Next(0, 3));
                    return new SpecResult(spec, SpecStatus.Passed, path);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    throw;
                }
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount * 2);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(exceptions).IsEmpty();
        await Assert.That(results).Count().IsEqualTo(HighConcurrencySpecCount);
    }

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_100ParallelSpecs_AllResultsHaveCorrectStatus()
    {
        var context = new SpecContext("stress-test");
        var expectedStatuses = new SpecStatus[HighConcurrencySpecCount];

        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            // Alternate between passed and failed
            var status = i % 3 == 0 ? SpecStatus.Failed : SpecStatus.Passed;
            expectedStatuses[i] = status;

            var index = i;
            context.AddSpec(new SpecDefinition($"spec-{i}", () =>
            {
                if (expectedStatuses[index] == SpecStatus.Failed)
                    throw new InvalidOperationException("Expected failure");
            }));
        }

        var results = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                await Task.Delay(Random.Shared.Next(0, 2));
                var index = int.Parse(spec.Description.Split('-')[1]);

                if (expectedStatuses[index] == SpecStatus.Failed)
                    return new SpecResult(spec, SpecStatus.Failed, path);

                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            await Assert.That(results[i].Status).IsEqualTo(expectedStatuses[i]);
        }
    }

    #endregion

    #region Bail Mode Under Load Tests

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_BailModeWithHighParallelism_SkipsRemainingSpecs()
    {
        var context = new SpecContext("bail-stress");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var bailTriggered = false;
        var results = new List<SpecResult>();
        var executionCount = 0;

        var strategyContext = CreateContext(
            context,
            results,
            bailEnabled: true,
            isBailTriggered: () => Volatile.Read(ref bailTriggered),
            signalBail: () => Volatile.Write(ref bailTriggered, true),
            runSpec: async (spec, ctx, path, focused) =>
            {
                var count = Interlocked.Increment(ref executionCount);
                await Task.Delay(Random.Shared.Next(1, 10));

                // Fail on the 5th spec to trigger bail
                if (count == 5)
                    return new SpecResult(spec, SpecStatus.Failed, path);

                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(results).Count().IsEqualTo(HighConcurrencySpecCount);
        await Assert.That(bailTriggered).IsTrue();

        // Count skipped specs - should have some skipped due to bail
        var skippedCount = results.Count(r => r.Status == SpecStatus.Skipped);
        var passedCount = results.Count(r => r.Status == SpecStatus.Passed);
        var failedCount = results.Count(r => r.Status == SpecStatus.Failed);

        // At least one failure
        await Assert.That(failedCount).IsGreaterThanOrEqualTo(1);

        // Total should equal spec count
        await Assert.That(skippedCount + passedCount + failedCount).IsEqualTo(HighConcurrencySpecCount);
    }

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_BailModeFirstSpecFails_MaximizesSkipped()
    {
        var context = new SpecContext("bail-first");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var bailTriggered = false;
        var results = new List<SpecResult>();
        var firstSpecStarted = new TaskCompletionSource<bool>();

        var strategyContext = CreateContext(
            context,
            results,
            bailEnabled: true,
            isBailTriggered: () => Volatile.Read(ref bailTriggered),
            signalBail: () => Volatile.Write(ref bailTriggered, true),
            runSpec: async (spec, ctx, path, focused) =>
            {
                if (spec.Description == "spec-0")
                {
                    firstSpecStarted.TrySetResult(true);
                    return new SpecResult(spec, SpecStatus.Failed, path);
                }

                // Other specs wait briefly to let spec-0 fail first
                await Task.Delay(5);
                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        // Use high parallelism
        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount * 2);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(results).Count().IsEqualTo(HighConcurrencySpecCount);

        var skippedCount = results.Count(r => r.Status == SpecStatus.Skipped);

        // Should have many skipped specs (exact count varies due to race)
        await Assert.That(skippedCount).IsGreaterThan(0);
    }

    #endregion

    #region Cancellation Propagation Tests

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_CancellationMidExecution_AllSpecsObserveCancellation()
    {
        var context = new SpecContext("cancellation-stress");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        using var cts = new CancellationTokenSource();
        var results = new List<SpecResult>();
        var executionCount = 0;
        var cancelledDuringExecution = new ConcurrentBag<string>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                var count = Interlocked.Increment(ref executionCount);

                // Cancel after 10 specs start
                if (count == 10)
                    cts.Cancel();

                try
                {
                    await Task.Delay(50, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    cancelledDuringExecution.Add(spec.Description);
                    throw;
                }

                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await strategy.ExecuteAsync(strategyContext, cts.Token));

        // Some specs should have observed cancellation
        await Assert.That(cancelledDuringExecution.Count).IsGreaterThan(0);
    }

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_ImmediateCancellation_NoSpecsComplete()
    {
        var context = new SpecContext("immediate-cancel");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var results = new List<SpecResult>();
        var executedSpecs = new ConcurrentBag<string>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                executedSpecs.Add(spec.Description);
                await Task.Delay(10);
                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await strategy.ExecuteAsync(strategyContext, cts.Token));

        // Very few or no specs should have executed
        await Assert.That(executedSpecs.Count).IsLessThan(HighConcurrencySpecCount);
    }

    #endregion

    #region Reporter Notification Tests

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_BatchNotification_ContainsAllResultsInOrder()
    {
        var context = new SpecContext("notification-stress");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var results = new List<SpecResult>();
        IReadOnlyList<SpecResult>? batchResults = null;

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                await Task.Delay(Random.Shared.Next(1, 5));
                return new SpecResult(spec, SpecStatus.Passed, path);
            },
            notifyBatchCompleted: batch =>
            {
                batchResults = batch;
                return Task.CompletedTask;
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        await Assert.That(batchResults).IsNotNull();
        await Assert.That(batchResults!.Count).IsEqualTo(HighConcurrencySpecCount);

        // Verify batch is in correct order
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            await Assert.That(batchResults[i].Spec.Description).IsEqualTo($"spec-{i}");
        }
    }

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_BatchNotification_NoDuplicates()
    {
        var context = new SpecContext("no-duplicates");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var results = new List<SpecResult>();
        var notifiedDescriptions = new ConcurrentBag<string>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                await Task.Delay(Random.Shared.Next(0, 3));
                return new SpecResult(spec, SpecStatus.Passed, path);
            },
            notifyBatchCompleted: batch =>
            {
                foreach (var result in batch)
                    notifiedDescriptions.Add(result.Spec.Description);
                return Task.CompletedTask;
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        var uniqueDescriptions = notifiedDescriptions.Distinct().ToList();
        await Assert.That(uniqueDescriptions.Count).IsEqualTo(notifiedDescriptions.Count);
    }

    #endregion

    #region State Isolation Tests

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_ParallelSpecs_NoSharedStateCorruption()
    {
        var context = new SpecContext("isolation-test");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var results = new List<SpecResult>();
        var capturedIndices = new ConcurrentDictionary<string, int>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                // Extract index from spec description
                var expectedIndex = int.Parse(spec.Description.Split('-')[1]);

                // Simulate work that could expose shared state issues
                await Task.Delay(Random.Shared.Next(1, 5));

                // Re-extract to verify no corruption
                var actualIndex = int.Parse(spec.Description.Split('-')[1]);
                capturedIndices[spec.Description] = actualIndex;

                if (expectedIndex != actualIndex)
                    return new SpecResult(spec, SpecStatus.Failed, path);

                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount * 2);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        // All specs should pass (no state corruption)
        var failedCount = results.Count(r => r.Status == SpecStatus.Failed);
        await Assert.That(failedCount).IsEqualTo(0);

        // Verify each spec captured its own index
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            var key = $"spec-{i}";
            await Assert.That(capturedIndices.ContainsKey(key)).IsTrue();
            await Assert.That(capturedIndices[key]).IsEqualTo(i);
        }
    }

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_ParallelSpecs_ThreadLocalStateIsolated()
    {
        var context = new SpecContext("thread-local-test");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var results = new List<SpecResult>();
        var threadLocalValue = new AsyncLocal<int>();
        var capturedValues = new ConcurrentDictionary<string, int>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                var index = int.Parse(spec.Description.Split('-')[1]);

                // Set thread-local value
                threadLocalValue.Value = index;

                // Yield to allow other specs to run
                await Task.Delay(Random.Shared.Next(1, 5));

                // Capture the value - should still be our value
                capturedValues[spec.Description] = threadLocalValue.Value;

                // Verify isolation
                if (threadLocalValue.Value != index)
                    return new SpecResult(spec, SpecStatus.Failed, path);

                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        // All specs should pass (AsyncLocal properly isolated)
        var failedCount = results.Count(r => r.Status == SpecStatus.Failed);
        await Assert.That(failedCount).IsEqualTo(0);

        // Each spec should have captured its own value
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            var key = $"spec-{i}";
            await Assert.That(capturedValues[key]).IsEqualTo(i);
        }
    }

    #endregion

    #region Contention Tests

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_HighContention_ResultListIntegrity()
    {
        var context = new SpecContext("contention-test");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var results = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                // Minimal delay to maximize contention
                await Task.Yield();
                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        // Use high parallelism to maximize contention
        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount * 4);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        // Results list should have exactly the right count
        await Assert.That(results).Count().IsEqualTo(HighConcurrencySpecCount);

        // No null entries
        await Assert.That(results.All(r => r != null)).IsTrue();

        // All specs accounted for
        var descriptions = results.Select(r => r.Spec.Description).ToHashSet();
        await Assert.That(descriptions.Count).IsEqualTo(HighConcurrencySpecCount);
    }

    [Test]
    [Repeat(StressIterations)]
    public async Task Execute_MixedDurations_PreservesOrdering()
    {
        var context = new SpecContext("mixed-duration");
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        var results = new List<SpecResult>();

        var strategyContext = CreateContext(
            context,
            results,
            runSpec: async (spec, ctx, path, focused) =>
            {
                var index = int.Parse(spec.Description.Split('-')[1]);

                // Vary delays significantly based on index
                var delay = index % 10 == 0 ? 50 : index % 5 == 0 ? 20 : 1;
                await Task.Delay(delay);

                return new SpecResult(spec, SpecStatus.Passed, path);
            });

        var strategy = new ParallelExecutionStrategy(Environment.ProcessorCount);
        await strategy.ExecuteAsync(strategyContext, CancellationToken.None);

        // Despite varied completion times, results should be in order
        for (var i = 0; i < HighConcurrencySpecCount; i++)
        {
            await Assert.That(results[i].Spec.Description).IsEqualTo($"spec-{i}");
        }
    }

    #endregion

    #region Helper Methods

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
            ContextPath = [],
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

    #endregion
}

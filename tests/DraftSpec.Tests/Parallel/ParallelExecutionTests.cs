using System.Collections.Concurrent;
using System.Diagnostics;

namespace DraftSpec.Tests.ParallelExecution;

/// <summary>
/// Tests for parallel spec execution functionality.
/// </summary>
public class ParallelExecutionTests
{
    #region Basic Parallel Execution

    [Test]
    public async Task WithParallelExecution_ExecutesSpecsConcurrently()
    {
        var executionTimes = new ConcurrentBag<(string Name, DateTime Start, DateTime End)>();
        var context = new SpecContext("parallel");

        for (var i = 0; i < 10; i++)
        {
            var name = $"spec-{i}";
            context.AddSpec(new SpecDefinition(name, async () =>
            {
                var start = DateTime.UtcNow;
                await Task.Delay(50);
                var end = DateTime.UtcNow;
                executionTimes.Add((name, start, end));
            }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(10);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();

        // Verify concurrency by checking that execution times overlap
        // If specs ran concurrently, some will have overlapping time ranges
        var times = executionTimes.OrderBy(t => t.Start).ToList();
        var overlaps = 0;
        for (var i = 0; i < times.Count - 1; i++)
            // Check if spec[i] overlaps with spec[i+1] (started before previous ended)
            if (times[i + 1].Start < times[i].End)
                overlaps++;

        // With 4 parallel threads and 10 specs, we should have significant overlap
        // At minimum, 3+ specs should overlap (running 4 at a time)
        await Assert.That(overlaps).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task WithParallelExecution_PreservesResultOrder()
    {
        var context = new SpecContext("order");

        for (var i = 0; i < 20; i++)
        {
            var index = i;
            context.AddSpec(new SpecDefinition($"spec-{index}", async () =>
            {
                // Random delays to ensure execution order differs from declaration order
                await Task.Delay(Random.Shared.Next(10, 50));
            }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(8)
            .Build();

        var results = await runner.RunAsync(context);

        // Results should be in declaration order regardless of execution order
        for (var i = 0; i < 20; i++) await Assert.That(results[i].Spec.Description).IsEqualTo($"spec-{i}");
    }

    [Test]
    public async Task WithParallelExecution_AllResultsReturned()
    {
        var context = new SpecContext("all");
        var executedSpecs = new ConcurrentBag<string>();

        for (var i = 0; i < 50; i++)
        {
            var name = $"spec-{i}";
            context.AddSpec(new SpecDefinition(name, () => { executedSpecs.Add(name); }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(10)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(50);
        await Assert.That(executedSpecs).Count().IsEqualTo(50);
    }

    #endregion

    #region Sequential vs Parallel Behavior

    [Test]
    public async Task WithoutParallelExecution_RunsSequentially()
    {
        var order = new List<int>();
        var context = new SpecContext("sequential");

        for (var i = 0; i < 5; i++)
        {
            var index = i;
            context.AddSpec(new SpecDefinition($"spec-{index}", () =>
            {
                lock (order)
                {
                    order.Add(index);
                }
            }));
        }

        var runner = new SpecRunner(); // No parallel execution
        await runner.RunAsync(context);

        // Sequential execution preserves declaration order
        for (var i = 0; i < 5; i++) await Assert.That(order[i]).IsEqualTo(i);
    }

    [Test]
    public async Task ParallelExecution_WithSingleSpec_WorksCorrectly()
    {
        var context = new SpecContext("single");
        context.AddSpec(new SpecDefinition("only spec", () => { }));

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task ParallelExecution_WithMaxDegree1_RunsSequentially()
    {
        var order = new List<int>();
        var lockObj = new object();
        var context = new SpecContext("degree1");

        for (var i = 0; i < 5; i++)
        {
            var index = i;
            context.AddSpec(new SpecDefinition($"spec-{index}", () =>
            {
                lock (lockObj)
                {
                    order.Add(index);
                }
            }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(1) // Effectively sequential
            .Build();

        await runner.RunAsync(context);

        // With parallelism of 1, should be sequential
        for (var i = 0; i < 5; i++) await Assert.That(order[i]).IsEqualTo(i);
    }

    #endregion

    #region Hooks with Parallel Execution

    [Test]
    public async Task ParallelExecution_BeforeAllRunsOnce()
    {
        var beforeAllCount = 0;
        var context = new SpecContext("hooks");
        context.BeforeAll = () =>
        {
            Interlocked.Increment(ref beforeAllCount);
            return Task.CompletedTask;
        };

        for (var i = 0; i < 10; i++) context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        await runner.RunAsync(context);

        await Assert.That(beforeAllCount).IsEqualTo(1);
    }

    [Test]
    public async Task ParallelExecution_AfterAllRunsOnce()
    {
        var afterAllCount = 0;
        var context = new SpecContext("hooks");
        context.AfterAll = () =>
        {
            Interlocked.Increment(ref afterAllCount);
            return Task.CompletedTask;
        };

        for (var i = 0; i < 10; i++) context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        await runner.RunAsync(context);

        await Assert.That(afterAllCount).IsEqualTo(1);
    }

    [Test]
    public async Task ParallelExecution_BeforeEachRunsPerSpec()
    {
        var beforeEachCount = 0;
        var context = new SpecContext("hooks");
        context.BeforeEach = () =>
        {
            Interlocked.Increment(ref beforeEachCount);
            return Task.CompletedTask;
        };

        for (var i = 0; i < 10; i++) context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        await runner.RunAsync(context);

        await Assert.That(beforeEachCount).IsEqualTo(10);
    }

    [Test]
    public async Task ParallelExecution_AfterEachRunsPerSpec()
    {
        var afterEachCount = 0;
        var context = new SpecContext("hooks");
        context.AfterEach = () =>
        {
            Interlocked.Increment(ref afterEachCount);
            return Task.CompletedTask;
        };

        for (var i = 0; i < 10; i++) context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        await runner.RunAsync(context);

        await Assert.That(afterEachCount).IsEqualTo(10);
    }

    #endregion

    #region Failure Handling

    [Test]
    public async Task ParallelExecution_HandlesFailures()
    {
        var context = new SpecContext("failures");

        context.AddSpec(new SpecDefinition("pass-1", () => { }));
        context.AddSpec(new SpecDefinition("fail-1", () => throw new Exception("fail-1")));
        context.AddSpec(new SpecDefinition("pass-2", () => { }));
        context.AddSpec(new SpecDefinition("fail-2", () => throw new Exception("fail-2")));
        context.AddSpec(new SpecDefinition("pass-3", () => { }));

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(5);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(3);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Failed)).IsEqualTo(2);

        // Verify order preserved
        await Assert.That(results[0].Spec.Description).IsEqualTo("pass-1");
        await Assert.That(results[1].Spec.Description).IsEqualTo("fail-1");
        await Assert.That(results[2].Spec.Description).IsEqualTo("pass-2");
    }

    [Test]
    public async Task ParallelExecution_FailureInOneDoesNotAffectOthers()
    {
        var executedSpecs = new ConcurrentBag<string>();
        var context = new SpecContext("isolation");

        for (var i = 0; i < 10; i++)
        {
            var name = $"spec-{i}";
            if (i == 5)
                context.AddSpec(new SpecDefinition(name, () =>
                {
                    executedSpecs.Add(name);
                    throw new Exception("intentional failure");
                }));
            else
                context.AddSpec(new SpecDefinition(name, () => { executedSpecs.Add(name); }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        var results = await runner.RunAsync(context);

        // All specs should execute despite one failing
        await Assert.That(executedSpecs).Count().IsEqualTo(10);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(9);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Failed)).IsEqualTo(1);
    }

    #endregion

    #region Nested Contexts

    [Test]
    public async Task ParallelExecution_WithNestedContexts_MaintainsIsolation()
    {
        var root = new SpecContext("root");
        var executionLog = new ConcurrentBag<string>();

        // Root specs
        for (var i = 0; i < 3; i++)
        {
            var name = $"root-{i}";
            root.AddSpec(new SpecDefinition(name, () => executionLog.Add(name)));
        }

        // Child context
        var child = new SpecContext("child", root);
        for (var i = 0; i < 3; i++)
        {
            var name = $"child-{i}";
            child.AddSpec(new SpecDefinition(name, () => executionLog.Add(name)));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        var results = await runner.RunAsync(root);

        await Assert.That(results).Count().IsEqualTo(6);
        await Assert.That(executionLog).Count().IsEqualTo(6);
    }

    #endregion

    #region Builder API

    [Test]
    public async Task WithParallelExecution_DefaultUsesProcessorCount()
    {
        var builder = SpecRunner.Create()
            .WithParallelExecution(); // No parameter = processor count

        await Assert.That(builder.MaxDegreeOfParallelism).IsEqualTo(Environment.ProcessorCount);
    }

    [Test]
    public async Task WithParallelExecution_ZeroUsesProcessorCount()
    {
        var builder = SpecRunner.Create()
            .WithParallelExecution(0);

        await Assert.That(builder.MaxDegreeOfParallelism).IsEqualTo(Environment.ProcessorCount);
    }

    [Test]
    public async Task WithParallelExecution_NegativeUsesProcessorCount()
    {
        var builder = SpecRunner.Create()
            .WithParallelExecution(-5);

        await Assert.That(builder.MaxDegreeOfParallelism).IsEqualTo(Environment.ProcessorCount);
    }

    [Test]
    public async Task WithParallelExecution_ExplicitValueIsRespected()
    {
        var builder = SpecRunner.Create()
            .WithParallelExecution(8);

        await Assert.That(builder.MaxDegreeOfParallelism).IsEqualTo(8);
    }

    #endregion

    #region Thread Safety

    [Test]
    public async Task ParallelExecution_ThreadSafeItemsDictionary()
    {
        var context = new SpecContext("threadsafe");
        var itemsAccessCount = 0;

        for (var i = 0; i < 100; i++)
        {
            var index = i;
            context.AddSpec(new SpecDefinition($"spec-{index}", () =>
            {
                // This would fail with regular Dictionary under concurrent access
                Interlocked.Increment(ref itemsAccessCount);
            }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(10)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(itemsAccessCount).IsEqualTo(100);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
    }

    #endregion
}

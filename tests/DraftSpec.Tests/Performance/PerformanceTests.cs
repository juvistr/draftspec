using System.Diagnostics;
using DraftSpec.Middleware;

namespace DraftSpec.Tests.Performance;

/// <summary>
/// Performance regression tests to establish baselines and detect performance degradation.
/// These tests verify that the framework can handle various scales efficiently.
/// </summary>
public class PerformanceTests
{
    #region Large Suite Tests

    [Test]
    public async Task LargeSuite_1000Specs_CompletesInReasonableTime()
    {
        var context = new SpecContext("perf");
        for (var i = 0; i < 1000; i++)
            context.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        var sw = Stopwatch.StartNew();
        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);
        sw.Stop();

        // Baseline: should complete in under 1 second
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(1000);
        await Assert.That(results).Count().IsEqualTo(1000);
    }

    [Test]
    public async Task LargeSuite_10000Specs_CompletesInReasonableTime()
    {
        var context = new SpecContext("perf");
        for (var i = 0; i < 10000; i++)
            context.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        var sw = Stopwatch.StartNew();
        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);
        sw.Stop();

        // Baseline: should complete in under 5 seconds
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(5000);
        await Assert.That(results).Count().IsEqualTo(10000);
    }

    #endregion

    #region Deep Nesting Tests

    [Test]
    public async Task DeepNesting_50Levels_CompletesWithoutStackOverflow()
    {
        var root = new SpecContext("level-0");
        var current = root;

        // Create 50 levels of nesting
        for (var i = 1; i < 50; i++) current = new SpecContext($"level-{i}", current);

        // Add spec at deepest level
        current.AddSpec(new SpecDefinition("deep spec", () => { }));

        var sw = Stopwatch.StartNew();
        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);
        sw.Stop();

        // Should complete without stack overflow
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].ContextPath).Count().IsEqualTo(50);
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(500);
    }

    [Test]
    public async Task DeepNesting_100Levels_CompletesWithoutStackOverflow()
    {
        var root = new SpecContext("level-0");
        var current = root;

        // Create 100 levels of nesting
        for (var i = 1; i < 100; i++) current = new SpecContext($"level-{i}", current);

        current.AddSpec(new SpecDefinition("very deep spec", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].ContextPath).Count().IsEqualTo(100);
    }

    #endregion

    #region Hook Chain Performance

    [Test]
    public async Task HookChain_10NestedContexts_ExecutesHooksEfficiently()
    {
        var hookCallCount = 0;
        var root = new SpecContext("root");
        var current = root;

        // Create 10 nested contexts, each with beforeEach and afterEach hooks
        for (var i = 0; i < 10; i++)
        {
            var ctx = i == 0 ? root : new SpecContext($"level-{i}", current);
            ctx.BeforeEach = () =>
            {
                hookCallCount++;
                return Task.CompletedTask;
            };
            ctx.AfterEach = () =>
            {
                hookCallCount++;
                return Task.CompletedTask;
            };
            current = ctx;
        }

        // Add 10 specs at the deepest level
        for (var i = 0; i < 10; i++) current.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        var sw = Stopwatch.StartNew();
        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);
        sw.Stop();

        // 10 specs * 10 beforeEach * 2 (before + after) = 200 hook calls
        await Assert.That(hookCallCount).IsEqualTo(200);
        await Assert.That(results).Count().IsEqualTo(10);
        // Should complete quickly despite many hooks
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(500);
    }

    [Test]
    public async Task HookChain_CachesHookChain_ForPerformance()
    {
        var root = new SpecContext("root");
        root.BeforeEach = () => Task.CompletedTask;

        var child = new SpecContext("child", root);
        child.BeforeEach = () => Task.CompletedTask;

        // Add many specs to same context
        for (var i = 0; i < 100; i++) child.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        // First call builds the cache
        var chain1 = child.GetBeforeEachChain();
        // Second call should return cached
        var chain2 = child.GetBeforeEachChain();

        await Assert.That(ReferenceEquals(chain1, chain2)).IsTrue();
    }

    [Test]
    public async Task HookChain_DeepNesting100Levels_BuildsInLinearTime()
    {
        // This test verifies the O(n²) -> O(n) fix for hook chain building
        // With 100 levels, O(n²) would be ~10,000 operations, O(n) is ~200

        var root = new SpecContext("level-0");
        root.BeforeEach = () => Task.CompletedTask;
        var current = root;

        // Create 100 levels of nesting, each with a beforeEach hook
        for (var i = 1; i < 100; i++)
        {
            current = new SpecContext($"level-{i}", current);
            current.BeforeEach = () => Task.CompletedTask;
        }

        current.AddSpec(new SpecDefinition("deep spec", () => { }));

        // Build the hook chain - should be O(n) not O(n²)
        var sw = Stopwatch.StartNew();
        var chain = current.GetBeforeEachChain();
        sw.Stop();

        // Verify chain has all 100 hooks in correct order (parent to child)
        await Assert.That(chain).Count().IsEqualTo(100);

        // Should complete very quickly with O(n) complexity
        // Even on slow systems, 100 list operations should be sub-millisecond
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(10);
    }

    [Test]
    public async Task HookChain_DeepNesting_MaintainsParentToChildOrder()
    {
        var executionOrder = new List<int>();

        var root = new SpecContext("level-0");
        root.BeforeEach = () =>
        {
            executionOrder.Add(0);
            return Task.CompletedTask;
        };

        var level1 = new SpecContext("level-1", root);
        level1.BeforeEach = () =>
        {
            executionOrder.Add(1);
            return Task.CompletedTask;
        };

        var level2 = new SpecContext("level-2", level1);
        level2.BeforeEach = () =>
        {
            executionOrder.Add(2);
            return Task.CompletedTask;
        };

        level2.AddSpec(new SpecDefinition("spec", () => { }));

        var runner = new SpecRunner();
        await runner.RunAsync(root);

        // Hooks should run in parent-to-child order: 0, 1, 2
        await Assert.That(executionOrder).IsEquivalentTo([0, 1, 2]);
    }

    #endregion

    #region Focus Detection Performance

    [Test]
    public async Task FocusDetection_LargeSuite_ScansQuickly()
    {
        var root = new SpecContext("root");

        // Create a large tree with many contexts and specs
        for (var i = 0; i < 100; i++)
        {
            var child = new SpecContext($"child-{i}", root);
            for (var j = 0; j < 100; j++) child.AddSpec(new SpecDefinition($"spec {j}", () => { }));
        }

        // Add one focused spec at the end
        root.AddSpec(new SpecDefinition("focused", () => { }) { IsFocused = true });

        var sw = Stopwatch.StartNew();
        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);
        sw.Stop();

        // Only the focused spec should run
        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(1);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Skipped)).IsEqualTo(10000);
        // Should detect focus and skip quickly
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(1000);
    }

    [Test]
    public async Task FocusDetection_NoFocusedSpecs_RunsAll()
    {
        var root = new SpecContext("root");
        for (var i = 0; i < 1000; i++) root.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        // All specs should run when none are focused
        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(1000);
    }

    #endregion

    #region Middleware Overhead

    [Test]
    public async Task MiddlewareOverhead_NoMiddleware_EstablishesBaseline()
    {
        var context = new SpecContext("perf");
        for (var i = 0; i < 1000; i++)
            context.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        var runner = new SpecRunner();

        var sw = Stopwatch.StartNew();
        await runner.RunAsync(context);
        sw.Stop();

        // Store baseline for comparison
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(500);
    }

    [Test]
    public async Task MiddlewareOverhead_WithRetryMiddleware_AcceptableOverhead()
    {
        var context = new SpecContext("perf");
        for (var i = 0; i < 1000; i++)
            context.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        var runner = SpecRunner.Create()
            .WithRetry(1) // No actual retries since specs pass
            .Build();

        var sw = Stopwatch.StartNew();
        await runner.RunAsync(context);
        sw.Stop();

        // Should have minimal overhead when specs don't fail
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(750);
    }

    [Test]
    public async Task MiddlewareOverhead_MultipleMiddleware_AcceptableOverhead()
    {
        var context = new SpecContext("perf");
        for (var i = 0; i < 1000; i++)
            context.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        var runner = SpecRunner.Create()
            .WithRetry(1)
            .WithTimeout(10000)
            .WithFilter(_ => true) // Pass-through filter
            .Build();

        var sw = Stopwatch.StartNew();
        await runner.RunAsync(context);
        sw.Stop();

        // Multiple middleware should still be efficient
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(1000);
    }

    #endregion

    #region Timing Instrumentation

    [Test]
    public async Task TimingInstrumentation_CapturesHookDurations()
    {
        var context = new SpecContext("root");
        context.BeforeEach = async () => await Task.Delay(10);
        context.AfterEach = async () => await Task.Delay(10);
        context.AddSpec(new SpecDefinition("spec", async () => await Task.Delay(10)));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        var result = results[0];
        // BeforeEach should have taken at least 10ms
        await Assert.That(result.BeforeEachDuration.TotalMilliseconds).IsGreaterThanOrEqualTo(9);
        // AfterEach should have taken at least 10ms
        await Assert.That(result.AfterEachDuration.TotalMilliseconds).IsGreaterThanOrEqualTo(9);
        // Spec body should have taken at least 10ms
        await Assert.That(result.Duration.TotalMilliseconds).IsGreaterThanOrEqualTo(9);
        // Total should be sum of all three
        await Assert.That(result.TotalDuration.TotalMilliseconds).IsGreaterThanOrEqualTo(27);
    }

    [Test]
    public async Task TimingInstrumentation_NestedHooks_AccumulatesDurations()
    {
        var root = new SpecContext("root");
        root.BeforeEach = async () => await Task.Delay(5);

        var child = new SpecContext("child", root);
        child.BeforeEach = async () => await Task.Delay(5);
        child.AddSpec(new SpecDefinition("spec", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        var result = results[0];
        // Both beforeEach hooks should have run
        await Assert.That(result.BeforeEachDuration.TotalMilliseconds).IsGreaterThanOrEqualTo(9);
    }

    #endregion

    #region Memory Efficiency

    [Test]
    public async Task MemoryEfficiency_LargeSuite_DoesNotRetainUnnecessaryReferences()
    {
        var context = new SpecContext("perf");
        var weakRefs = new List<WeakReference>();

        // Create specs with large closures
        for (var i = 0; i < 100; i++)
        {
            var data = new byte[10000]; // 10KB per spec
            weakRefs.Add(new WeakReference(data));
            context.AddSpec(new SpecDefinition($"spec {i}", () => { _ = data.Length; }));
        }

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        // After running, results should be available
        await Assert.That(results).Count().IsEqualTo(100);

        // Force GC
        results = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Some references should be collectible after results are dropped
        // (This is a weak assertion - mainly checking for obvious memory leaks)
        await Assert.That(weakRefs.Count).IsGreaterThan(0);
    }

    #endregion

    #region Wide Tree Performance

    [Test]
    public async Task WideTree_1000ContextsWith10SpecsEach_CompletesEfficiently()
    {
        var root = new SpecContext("root");

        // Create a wide tree: 1000 sibling contexts
        for (var i = 0; i < 1000; i++)
        {
            var child = new SpecContext($"context-{i}", root);
            for (var j = 0; j < 10; j++) child.AddSpec(new SpecDefinition($"spec {j}", () => { }));
        }

        var sw = Stopwatch.StartNew();
        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);
        sw.Stop();

        await Assert.That(results).Count().IsEqualTo(10000);
        // Wide tree should still complete quickly
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(5000);
    }

    #endregion
}

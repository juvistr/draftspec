namespace DraftSpec.Tests.EdgeCases;

/// <summary>
/// Edge case tests for unusual scenarios and boundary conditions.
/// </summary>
public class EdgeCaseTests
{
    #region Hook Exceptions

    [Test]
    public async Task BeforeAll_exception_prevents_specs_from_running()
    {
        var specRan = false;
        var context = new SpecContext("context");
        context.AddBeforeAll(() => throw new InvalidOperationException("BeforeAll failed"));
        context.AddSpec(new SpecDefinition("should not run", () => specRan = true));

        var runner = new SpecRunner();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(context));

        await Assert.That(ex!.Message).IsEqualTo("BeforeAll failed");
        await Assert.That(specRan).IsFalse();
    }

    [Test]
    public async Task BeforeEach_exception_propagates()
    {
        var context = new SpecContext("context");
        context.AddBeforeEach(() => throw new InvalidOperationException("BeforeEach failed"));
        context.AddSpec(new SpecDefinition("should fail", () => { }));

        var runner = new SpecRunner();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(context));

        await Assert.That(ex!.Message).IsEqualTo("BeforeEach failed");
    }

    [Test]
    public async Task AfterEach_exception_propagates_after_spec_runs()
    {
        var specRan = false;
        var context = new SpecContext("context");
        context.AddAfterEach(() => throw new InvalidOperationException("AfterEach failed"));
        context.AddSpec(new SpecDefinition("runs but afterEach fails", () => specRan = true));

        var runner = new SpecRunner();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(context));

        await Assert.That(specRan).IsTrue();
        await Assert.That(ex!.Message).IsEqualTo("AfterEach failed");
    }

    [Test]
    public async Task AfterAll_exception_propagates_after_specs_complete()
    {
        var specsRan = new List<string>();
        var context = new SpecContext("context");
        context.AddAfterAll(() => throw new InvalidOperationException("AfterAll failed"));
        context.AddSpec(new SpecDefinition("first spec", () => specsRan.Add("first")));
        context.AddSpec(new SpecDefinition("second spec", () => specsRan.Add("second")));

        var runner = new SpecRunner();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(context));

        await Assert.That(ex!.Message).IsEqualTo("AfterAll failed");
        await Assert.That(specsRan).IsEquivalentTo(["first", "second"]);
    }

    [Test]
    public async Task Multiple_hooks_first_one_throws_stops_execution()
    {
        var secondHookRan = false;
        var outer = new SpecContext("outer");
        outer.AddBeforeEach(() => throw new InvalidOperationException("First hook"));

        var inner = new SpecContext("inner", outer);
        inner.AddBeforeEach(() =>
        {
            secondHookRan = true;
            return Task.CompletedTask;
        });
        inner.AddSpec(new SpecDefinition("spec", () => { }));

        var runner = new SpecRunner();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(outer));

        await Assert.That(ex!.Message).IsEqualTo("First hook");
        await Assert.That(secondHookRan).IsFalse();
    }

    [Test]
    public async Task Spec_throws_AfterEach_still_runs()
    {
        var afterEachRan = false;
        var context = new SpecContext("context");
        context.AddAfterEach(() =>
        {
            afterEachRan = true;
            return Task.CompletedTask;
        });
        context.AddSpec(new SpecDefinition("throws", () => throw new InvalidOperationException("Spec failed")));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(afterEachRan).IsTrue();
    }

    [Test]
    public async Task BeforeEach_throws_AfterEach_does_not_run()
    {
        // When BeforeEach throws, AfterEach doesn't run (the spec hasn't started)
        var afterEachRan = false;
        var specRan = false;
        var context = new SpecContext("context");
        context.AddBeforeEach(() => throw new InvalidOperationException("BeforeEach failed"));
        context.AddAfterEach(() =>
        {
            afterEachRan = true;
            return Task.CompletedTask;
        });
        context.AddSpec(new SpecDefinition("should not run", () => specRan = true));

        var runner = new SpecRunner();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(context));

        await Assert.That(ex!.Message).IsEqualTo("BeforeEach failed");
        await Assert.That(specRan).IsFalse();
        // AfterEach does NOT run when BeforeEach throws (spec never started)
        await Assert.That(afterEachRan).IsFalse();
    }

    [Test]
    public async Task Nested_hooks_afterEach_runs_in_child_to_parent_order()
    {
        var hookOrder = new List<string>();

        var parent = new SpecContext("parent");
        parent.AddAfterEach(() =>
        {
            hookOrder.Add("after-parent");
            return Task.CompletedTask;
        });

        var child = new SpecContext("child", parent);
        child.AddAfterEach(() =>
        {
            hookOrder.Add("after-child");
            return Task.CompletedTask;
        });
        child.AddSpec(new SpecDefinition("spec", () => hookOrder.Add("spec")));

        var runner = new SpecRunner();
        await runner.RunAsync(parent);

        await Assert.That(hookOrder).IsEquivalentTo(["spec", "after-child", "after-parent"]);
    }

    #endregion

    #region Deep Nesting

    [Test]
    public async Task Deep_nesting_10_levels_works()
    {
        var root = new SpecContext("level-0");
        var current = root;

        for (var i = 1; i < 10; i++) current = new SpecContext($"level-{i}", current);
        current.AddSpec(new SpecDefinition("deepest spec", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task Deep_nesting_context_path_is_accurate()
    {
        var level1 = new SpecContext("level1");
        var level2 = new SpecContext("level2", level1);
        var level3 = new SpecContext("level3", level2);
        var level4 = new SpecContext("level4", level3);
        var level5 = new SpecContext("level5", level4);
        level5.AddSpec(new SpecDefinition("deep spec", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(level1);

        await Assert.That(results[0].ContextPath).IsEquivalentTo([
            "level1", "level2", "level3", "level4", "level5"
        ]);
    }

    [Test]
    public async Task Deep_nesting_hooks_inherited_correctly()
    {
        var hookOrder = new List<string>();

        var l1 = new SpecContext("L1");
        l1.AddBeforeEach(() =>
        {
            hookOrder.Add("before-L1");
            return Task.CompletedTask;
        });

        var l2 = new SpecContext("L2", l1);
        l2.AddBeforeEach(() =>
        {
            hookOrder.Add("before-L2");
            return Task.CompletedTask;
        });

        var l3 = new SpecContext("L3", l2);
        l3.AddBeforeEach(() =>
        {
            hookOrder.Add("before-L3");
            return Task.CompletedTask;
        });
        l3.AddSpec(new SpecDefinition("spec", () => hookOrder.Add("spec")));

        var runner = new SpecRunner();
        await runner.RunAsync(l1);

        await Assert.That(hookOrder).IsEquivalentTo([
            "before-L1", "before-L2", "before-L3", "spec"
        ]);
    }

    [Test]
    public async Task Deep_nesting_does_not_cause_stack_overflow()
    {
        var root = new SpecContext("level-0");
        var current = root;

        for (var i = 1; i < 50; i++) current = new SpecContext($"level-{i}", current);
        current.AddSpec(new SpecDefinition("bottom", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    #region Large Suites

    [Test]
    public async Task Large_suite_100_specs_in_single_context()
    {
        var context = new SpecContext("many specs");

        for (var i = 0; i < 100; i++) context.AddSpec(new SpecDefinition($"spec {i}", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(100);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
    }

    [Test]
    public async Task Large_suite_50_contexts()
    {
        var root = new SpecContext("root");

        for (var i = 0; i < 50; i++)
        {
            var child = new SpecContext($"context {i}", root);
            child.AddSpec(new SpecDefinition($"spec in context {i}", () => { }));
        }

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        await Assert.That(results).Count().IsEqualTo(50);
    }

    [Test]
    public async Task Large_suite_mixed_statuses()
    {
        var context = new SpecContext("mixed");

        for (var i = 0; i < 20; i++)
        {
            var n = i;
            if (n % 4 == 0)
                context.AddSpec(new SpecDefinition($"passes {n}", () => { }));
            else if (n % 4 == 1)
                context.AddSpec(new SpecDefinition($"fails {n}", () => throw new Exception("fail")));
            else if (n % 4 == 2)
                context.AddSpec(new SpecDefinition($"pending {n}")); // No body = pending
            else
                context.AddSpec(new SpecDefinition($"skipped {n}", () => { }) { IsSkipped = true });
        }

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(5);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Failed)).IsEqualTo(5);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Pending)).IsEqualTo(5);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Skipped)).IsEqualTo(5);
    }

    #endregion

    #region Boundary Conditions

    [Test]
    public async Task Empty_context_description_throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new SpecContext(""));

        await Assert.That(ex!.ParamName).IsEqualTo("description");
    }

    [Test]
    public async Task Empty_spec_description_allowed()
    {
        var context = new SpecContext("context");
        context.AddSpec(new SpecDefinition("", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Spec.Description).IsEqualTo("");
    }

    [Test]
    public async Task Unicode_in_descriptions()
    {
        var context = new SpecContext("数学测试 🧮");
        context.AddSpec(new SpecDefinition("1 + 1 = 2 ✓", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Spec.Description).IsEqualTo("1 + 1 = 2 ✓");
        await Assert.That(results[0].ContextPath[0]).IsEqualTo("数学测试 🧮");
    }

    [Test]
    public async Task Very_long_description()
    {
        var longDesc = new string('a', 10000);
        var context = new SpecContext(longDesc);
        context.AddSpec(new SpecDefinition("spec", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].ContextPath[0]).IsEqualTo(longDesc);
    }

    [Test]
    public async Task Spec_with_null_body_is_pending()
    {
        var context = new SpecContext("context");
        context.AddSpec(new SpecDefinition("pending spec")); // No body

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Pending);
    }

    [Test]
    public async Task Context_with_no_specs_produces_no_results()
    {
        var context = new SpecContext("empty context");

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results).IsEmpty();
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task Spec_throwing_null_exception()
    {
        var context = new SpecContext("context");
        context.AddSpec(new SpecDefinition("throws null", () => throw null!));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
    }

    [Test]
    public async Task Spec_throwing_aggregate_exception()
    {
        var context = new SpecContext("context");
        context.AddSpec(new SpecDefinition("throws aggregate", () =>
        {
            throw new AggregateException(
                new InvalidOperationException("inner1"),
                new ArgumentException("inner2"));
        }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception).IsTypeOf<AggregateException>();
    }

    [Test]
    public async Task Spec_throwing_task_canceled_exception()
    {
        var context = new SpecContext("context");
        context.AddSpec(new SpecDefinition("canceled",
            () => throw new TaskCanceledException("Operation was canceled")));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception).IsTypeOf<TaskCanceledException>();
    }

    #endregion
}

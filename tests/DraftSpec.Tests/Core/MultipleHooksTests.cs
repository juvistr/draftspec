using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Core;

public class MultipleHooksTests
{
    #region Multiple beforeEach hooks

    [Test]
    public async Task Multiple_beforeEach_hooks_all_execute_in_declaration_order()
    {
        var order = new List<string>();
        var context = new SpecContext("test");
        context.AddBeforeEach(() => { order.Add("before1"); return Task.CompletedTask; });
        context.AddBeforeEach(() => { order.Add("before2"); return Task.CompletedTask; });
        context.AddSpec(new SpecDefinition("spec", () => order.Add("spec")));

        await new SpecRunner().RunAsync(context);

        await Assert.That(order).IsEquivalentTo(["before1", "before2", "spec"]);
    }

    #endregion

    #region Multiple afterEach hooks

    [Test]
    public async Task Multiple_afterEach_hooks_execute_in_reverse_declaration_order()
    {
        var order = new List<string>();
        var context = new SpecContext("test");
        context.AddAfterEach(() => { order.Add("after1"); return Task.CompletedTask; });
        context.AddAfterEach(() => { order.Add("after2"); return Task.CompletedTask; });
        context.AddSpec(new SpecDefinition("spec", () => order.Add("spec")));

        await new SpecRunner().RunAsync(context);

        await Assert.That(order).IsEquivalentTo(["spec", "after2", "after1"]);
    }

    #endregion

    #region Multiple beforeAll hooks

    [Test]
    public async Task Multiple_beforeAll_hooks_execute_once_in_declaration_order()
    {
        var order = new List<string>();
        var context = new SpecContext("test");
        context.AddBeforeAll(() => { order.Add("beforeAll1"); return Task.CompletedTask; });
        context.AddBeforeAll(() => { order.Add("beforeAll2"); return Task.CompletedTask; });
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));

        await new SpecRunner().RunAsync(context);

        await Assert.That(order).IsEquivalentTo(["beforeAll1", "beforeAll2"]);
    }

    #endregion

    #region Multiple afterAll hooks

    [Test]
    public async Task Multiple_afterAll_hooks_execute_once_in_reverse_order()
    {
        var order = new List<string>();
        var context = new SpecContext("test");
        context.AddAfterAll(() => { order.Add("afterAll1"); return Task.CompletedTask; });
        context.AddAfterAll(() => { order.Add("afterAll2"); return Task.CompletedTask; });
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));

        await new SpecRunner().RunAsync(context);

        await Assert.That(order).IsEquivalentTo(["afterAll2", "afterAll1"]);
    }

    #endregion

    #region Cross-context with multiple hooks

    [Test]
    public async Task Multiple_beforeEach_with_nesting_parent_fifo_then_child_fifo()
    {
        var order = new List<string>();
        var parent = new SpecContext("parent");
        parent.AddBeforeEach(() => { order.Add("parent-b1"); return Task.CompletedTask; });
        parent.AddBeforeEach(() => { order.Add("parent-b2"); return Task.CompletedTask; });

        var child = new SpecContext("child", parent);
        child.AddBeforeEach(() => { order.Add("child-b1"); return Task.CompletedTask; });
        child.AddBeforeEach(() => { order.Add("child-b2"); return Task.CompletedTask; });
        child.AddSpec(new SpecDefinition("spec", () => order.Add("spec")));

        await new SpecRunner().RunAsync(parent);

        await Assert.That(order).IsEquivalentTo([
            "parent-b1", "parent-b2", "child-b1", "child-b2", "spec"
        ]);
    }

    [Test]
    public async Task Multiple_afterEach_with_nesting_child_lifo_then_parent_lifo()
    {
        var order = new List<string>();
        var parent = new SpecContext("parent");
        parent.AddAfterEach(() => { order.Add("parent-a1"); return Task.CompletedTask; });
        parent.AddAfterEach(() => { order.Add("parent-a2"); return Task.CompletedTask; });

        var child = new SpecContext("child", parent);
        child.AddAfterEach(() => { order.Add("child-a1"); return Task.CompletedTask; });
        child.AddAfterEach(() => { order.Add("child-a2"); return Task.CompletedTask; });
        child.AddSpec(new SpecDefinition("spec", () => order.Add("spec")));

        await new SpecRunner().RunAsync(parent);

        await Assert.That(order).IsEquivalentTo([
            "spec", "child-a2", "child-a1", "parent-a2", "parent-a1"
        ]);
    }

    #endregion

    #region DSL integration

    [Before(Test)]
    public void ResetDslState() => Reset();

    [Test]
    public async Task Dsl_multiple_before_hooks_accumulate()
    {
        var order = new List<string>();
        describe("test", () =>
        {
            before(() => order.Add("before1"));
            before(() => order.Add("before2"));
            it("spec", () => order.Add("spec"));
        });

        await new SpecRunner().RunAsync(RootContext!);
        await Assert.That(order).IsEquivalentTo(["before1", "before2", "spec"]);
    }

    [Test]
    public async Task Dsl_multiple_after_hooks_accumulate_in_lifo()
    {
        var order = new List<string>();
        describe("test", () =>
        {
            after(() => order.Add("after1"));
            after(() => order.Add("after2"));
            it("spec", () => order.Add("spec"));
        });

        await new SpecRunner().RunAsync(RootContext!);
        await Assert.That(order).IsEquivalentTo(["spec", "after2", "after1"]);
    }

    [Test]
    public async Task Dsl_multiple_beforeAll_hooks_accumulate()
    {
        var order = new List<string>();
        describe("test", () =>
        {
            beforeAll(() => order.Add("beforeAll1"));
            beforeAll(() => order.Add("beforeAll2"));
            it("spec1", () => { });
            it("spec2", () => { });
        });

        await new SpecRunner().RunAsync(RootContext!);
        await Assert.That(order).IsEquivalentTo(["beforeAll1", "beforeAll2"]);
    }

    [Test]
    public async Task Dsl_multiple_afterAll_hooks_accumulate_in_lifo()
    {
        var order = new List<string>();
        describe("test", () =>
        {
            afterAll(() => order.Add("afterAll1"));
            afterAll(() => order.Add("afterAll2"));
            it("spec1", () => { });
            it("spec2", () => { });
        });

        await new SpecRunner().RunAsync(RootContext!);
        await Assert.That(order).IsEquivalentTo(["afterAll2", "afterAll1"]);
    }

    #endregion

    #region Backward compatibility

    [Test]
    public async Task Single_hook_still_works()
    {
        var ran = false;
        var context = new SpecContext("test");
        context.AddBeforeEach(() => { ran = true; return Task.CompletedTask; });
        context.AddSpec(new SpecDefinition("spec", () => { }));

        await new SpecRunner().RunAsync(context);
        await Assert.That(ran).IsTrue();
    }

    [Test]
    public async Task No_hooks_still_works()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec", () => { }));

        var results = await new SpecRunner().RunAsync(context);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion
}

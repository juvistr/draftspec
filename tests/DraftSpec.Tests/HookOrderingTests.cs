namespace DraftSpec.Tests;

public class HookOrderingTests
{
    [Test]
    public async Task BeforeEach_runs_parent_to_child()
    {
        var executionOrder = new List<string>();

        var spec = new HookTestSpec(executionOrder);
        var runner = new SpecRunner();
        var results = await runner.RunAsync(spec);

        // Find the innermost spec result
        var innerSpec = results.First(r => r.Spec.Description == "runs in inner context");
        await Assert.That(innerSpec.Status).IsEqualTo(SpecStatus.Passed);

        // Verify beforeEach order: root → outer → inner → spec
        var beforeEachCalls = executionOrder
            .Where(e => e.StartsWith("beforeEach:"))
            .ToList();

        await Assert.That(beforeEachCalls).IsEquivalentTo([
            "beforeEach:root",
            "beforeEach:outer",
            "beforeEach:inner"
        ]);
    }

    [Test]
    public async Task AfterEach_runs_child_to_parent()
    {
        var executionOrder = new List<string>();

        var spec = new HookTestSpec(executionOrder);
        var runner = new SpecRunner();
        await runner.RunAsync(spec);

        // Verify afterEach order: inner → outer → root
        var afterEachCalls = executionOrder
            .Where(e => e.StartsWith("afterEach:"))
            .ToList();

        await Assert.That(afterEachCalls).IsEquivalentTo([
            "afterEach:inner",
            "afterEach:outer",
            "afterEach:root"
        ]);
    }

    [Test]
    public async Task BeforeAll_runs_once_per_context()
    {
        var executionOrder = new List<string>();

        var spec = new HookTestSpecWithMultipleSpecs(executionOrder);
        var runner = new SpecRunner();
        await runner.RunAsync(spec);

        // beforeAll should run once per context, not per spec
        var beforeAllCalls = executionOrder
            .Where(e => e.StartsWith("beforeAll:"))
            .ToList();

        await Assert.That(beforeAllCalls).IsEquivalentTo([
            "beforeAll:root",
            "beforeAll:context"
        ]);
    }

    [Test]
    public async Task Full_hook_order_is_correct()
    {
        var executionOrder = new List<string>();

        var spec = new HookTestSpec(executionOrder);
        var runner = new SpecRunner();
        await runner.RunAsync(spec);

        // Expected order for a single spec in nested contexts:
        // beforeAll(root) → beforeAll(outer) → beforeAll(inner)
        // → beforeEach(root) → beforeEach(outer) → beforeEach(inner)
        // → spec
        // → afterEach(inner) → afterEach(outer) → afterEach(root)
        // → afterAll(inner) → afterAll(outer) → afterAll(root)

        await Assert.That(executionOrder).IsEquivalentTo([
            "beforeAll:root",
            "beforeAll:outer",
            "beforeAll:inner",
            "beforeEach:root",
            "beforeEach:outer",
            "beforeEach:inner",
            "spec:inner",
            "afterEach:inner",
            "afterEach:outer",
            "afterEach:root",
            "afterAll:inner",
            "afterAll:outer",
            "afterAll:root"
        ]);
    }

    private class HookTestSpec : Spec
    {
        public HookTestSpec(List<string> executionOrder)
        {
            beforeAll = () => executionOrder.Add("beforeAll:root");
            afterAll = () => executionOrder.Add("afterAll:root");
            before = () => executionOrder.Add("beforeEach:root");
            after = () => executionOrder.Add("afterEach:root");

            describe("outer", () =>
            {
                beforeAll = () => executionOrder.Add("beforeAll:outer");
                afterAll = () => executionOrder.Add("afterAll:outer");
                before = () => executionOrder.Add("beforeEach:outer");
                after = () => executionOrder.Add("afterEach:outer");

                describe("inner", () =>
                {
                    beforeAll = () => executionOrder.Add("beforeAll:inner");
                    afterAll = () => executionOrder.Add("afterAll:inner");
                    before = () => executionOrder.Add("beforeEach:inner");
                    after = () => executionOrder.Add("afterEach:inner");

                    it("runs in inner context", () => { executionOrder.Add("spec:inner"); });
                });
            });
        }
    }

    private class HookTestSpecWithMultipleSpecs : Spec
    {
        public HookTestSpecWithMultipleSpecs(List<string> executionOrder)
        {
            beforeAll = () => executionOrder.Add("beforeAll:root");

            describe("context", () =>
            {
                beforeAll = () => executionOrder.Add("beforeAll:context");
                before = () => executionOrder.Add("beforeEach:context");

                it("first spec", () => executionOrder.Add("spec:first"));
                it("second spec", () => executionOrder.Add("spec:second"));
            });
        }
    }
}

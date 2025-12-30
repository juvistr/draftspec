namespace DraftSpec.Tests;

/// <summary>
/// Tests for the class-based Spec API (alternative to static Dsl).
/// </summary>
public class SpecClassTests
{
    #region Basic Spec Definition

    [Test]
    public async Task Spec_RootContext_NamedAfterClass()
    {
        var spec = new TestCalculatorSpec();

        await Assert.That(spec.RootContext.Description).IsEqualTo("TestCalculatorSpec");
    }

    [Test]
    public async Task Spec_Describe_CreatesNestedContext()
    {
        var spec = new DescribeTestSpec();

        await Assert.That(spec.RootContext.Children).Count().IsEqualTo(1);
        await Assert.That(spec.RootContext.Children[0].Description).IsEqualTo("Calculator");
    }

    [Test]
    public async Task Spec_Context_IsAliasForDescribe()
    {
        var spec = new ContextTestSpec();

        // The outer describe creates one child
        await Assert.That(spec.RootContext.Children).Count().IsEqualTo(1);
        // The context() call inside creates a nested child
        var outer = spec.RootContext.Children[0];
        await Assert.That(outer.Description).IsEqualTo("Calculator");
        await Assert.That(outer.Children).Count().IsEqualTo(1);
        await Assert.That(outer.Children[0].Description).IsEqualTo("when adding");
    }

    [Test]
    public async Task Spec_It_AddsSpecToContext()
    {
        var spec = new SimpleItSpec();

        var specs = spec.RootContext.Children[0].Specs;
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].Description).IsEqualTo("adds numbers");
    }

    [Test]
    public async Task Spec_ItAsync_AddsAsyncSpec()
    {
        var spec = new AsyncItSpec();

        var specs = spec.RootContext.Children[0].Specs;
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].Description).IsEqualTo("async spec");
    }

    [Test]
    public async Task Spec_ItPending_AddsPendingSpec()
    {
        var spec = new PendingItSpec();

        var specs = spec.RootContext.Children[0].Specs;
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].Description).IsEqualTo("pending spec");
        await Assert.That(specs[0].IsPending).IsTrue();
    }

    #endregion

    #region Focused and Skipped Specs

    [Test]
    public async Task Spec_Fit_AddsFocusedSpec()
    {
        var spec = new FitSpec();

        var specs = spec.RootContext.Children[0].Specs;
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].IsFocused).IsTrue();
    }

    [Test]
    public async Task Spec_FitAsync_AddsFocusedAsyncSpec()
    {
        var spec = new FitAsyncSpec();

        var specs = spec.RootContext.Children[0].Specs;
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].IsFocused).IsTrue();
    }

    [Test]
    public async Task Spec_Xit_AddsSkippedSpec()
    {
        var spec = new XitSpec();

        var specs = spec.RootContext.Children[0].Specs;
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].IsSkipped).IsTrue();
    }

    [Test]
    public async Task Spec_XitAsync_AddsSkippedAsyncSpec()
    {
        var spec = new XitAsyncSpec();

        var specs = spec.RootContext.Children[0].Specs;
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].IsSkipped).IsTrue();
    }

    #endregion

    #region Hooks

    [Test]
    public async Task Spec_BeforeAll_SetsHook()
    {
        var spec = new BeforeAllSpec();
        var hook = spec.RootContext.Children[0].BeforeAll;

        await Assert.That(hook is not null).IsTrue();
    }

    [Test]
    public async Task Spec_BeforeAllAsync_SetsAsyncHook()
    {
        var spec = new BeforeAllAsyncSpec();
        var hook = spec.RootContext.Children[0].BeforeAll;

        await Assert.That(hook is not null).IsTrue();
    }

    [Test]
    public async Task Spec_AfterAll_SetsHook()
    {
        var spec = new AfterAllSpec();
        var hook = spec.RootContext.Children[0].AfterAll;

        await Assert.That(hook is not null).IsTrue();
    }

    [Test]
    public async Task Spec_AfterAllAsync_SetsAsyncHook()
    {
        var spec = new AfterAllAsyncSpec();
        var hook = spec.RootContext.Children[0].AfterAll;

        await Assert.That(hook is not null).IsTrue();
    }

    [Test]
    public async Task Spec_Before_SetsBeforeEachHook()
    {
        var spec = new BeforeSpec();
        var hook = spec.RootContext.Children[0].BeforeEach;

        await Assert.That(hook is not null).IsTrue();
    }

    [Test]
    public async Task Spec_BeforeAsync_SetsAsyncBeforeEachHook()
    {
        var spec = new BeforeAsyncSpec();
        var hook = spec.RootContext.Children[0].BeforeEach;

        await Assert.That(hook is not null).IsTrue();
    }

    [Test]
    public async Task Spec_After_SetsAfterEachHook()
    {
        var spec = new AfterSpec();
        var hook = spec.RootContext.Children[0].AfterEach;

        await Assert.That(hook is not null).IsTrue();
    }

    [Test]
    public async Task Spec_AfterAsync_SetsAsyncAfterEachHook()
    {
        var spec = new AfterAsyncSpec();
        var hook = spec.RootContext.Children[0].AfterEach;

        await Assert.That(hook is not null).IsTrue();
    }

    #endregion

    #region Nested Contexts

    [Test]
    public async Task Spec_NestedDescribe_CreatesHierarchy()
    {
        var spec = new NestedDescribeSpec();

        var outer = spec.RootContext.Children[0];
        await Assert.That(outer.Description).IsEqualTo("outer");
        await Assert.That(outer.Children).Count().IsEqualTo(1);

        var inner = outer.Children[0];
        await Assert.That(inner.Description).IsEqualTo("inner");
        await Assert.That(inner.Specs).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Spec_MultipleDescribes_AllAdded()
    {
        var spec = new MultipleDescribeSpec();

        await Assert.That(spec.RootContext.Children).Count().IsEqualTo(2);
        await Assert.That(spec.RootContext.Children[0].Description).IsEqualTo("first");
        await Assert.That(spec.RootContext.Children[1].Description).IsEqualTo("second");
    }

    #endregion

    #region Execution

    [Test]
    public async Task Spec_CanBeExecuted_WithRunner()
    {
        var spec = new ExecutableSpec();
        var runner = new SpecRunner();

        var results = await runner.RunAsync(spec.RootContext);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    #region Test Spec Classes

    private class TestCalculatorSpec : Spec
    {
        public TestCalculatorSpec()
        {
            // Just uses default root context
        }
    }

    private class DescribeTestSpec : Spec
    {
        public DescribeTestSpec()
        {
            describe("Calculator", () =>
            {
                it("adds", () => { });
            });
        }
    }

    private class ContextTestSpec : Spec
    {
        public ContextTestSpec()
        {
            describe("Calculator", () =>
            {
                context("when adding", () =>
                {
                    it("returns sum", () => { });
                });
            });
        }
    }

    private class SimpleItSpec : Spec
    {
        public SimpleItSpec()
        {
            describe("Math", () =>
            {
                it("adds numbers", () => { });
            });
        }
    }

    private class AsyncItSpec : Spec
    {
        public AsyncItSpec()
        {
            describe("Async", () =>
            {
                it("async spec", async () => await Task.CompletedTask);
            });
        }
    }

    private class PendingItSpec : Spec
    {
        public PendingItSpec()
        {
            describe("Pending", () =>
            {
                it("pending spec");
            });
        }
    }

    private class FitSpec : Spec
    {
        public FitSpec()
        {
            describe("Focused", () =>
            {
                fit("focused spec", () => { });
            });
        }
    }

    private class FitAsyncSpec : Spec
    {
        public FitAsyncSpec()
        {
            describe("Focused", () =>
            {
                fit("focused async", async () => await Task.CompletedTask);
            });
        }
    }

    private class XitSpec : Spec
    {
        public XitSpec()
        {
            describe("Skipped", () =>
            {
                xit("skipped spec", () => { });
            });
        }
    }

    private class XitAsyncSpec : Spec
    {
        public XitAsyncSpec()
        {
            describe("Skipped", () =>
            {
                xit("skipped async", async () => await Task.CompletedTask);
            });
        }
    }

    private class BeforeAllSpec : Spec
    {
        public BeforeAllSpec()
        {
            describe("Hooks", () =>
            {
                beforeAll = () => { };
                it("spec", () => { });
            });
        }
    }

    private class BeforeAllAsyncSpec : Spec
    {
        public BeforeAllAsyncSpec()
        {
            describe("Hooks", () =>
            {
                beforeAllAsync(async () => await Task.CompletedTask);
                it("spec", () => { });
            });
        }
    }

    private class AfterAllSpec : Spec
    {
        public AfterAllSpec()
        {
            describe("Hooks", () =>
            {
                afterAll = () => { };
                it("spec", () => { });
            });
        }
    }

    private class AfterAllAsyncSpec : Spec
    {
        public AfterAllAsyncSpec()
        {
            describe("Hooks", () =>
            {
                afterAllAsync(async () => await Task.CompletedTask);
                it("spec", () => { });
            });
        }
    }

    private class BeforeSpec : Spec
    {
        public BeforeSpec()
        {
            describe("Hooks", () =>
            {
                before = () => { };
                it("spec", () => { });
            });
        }
    }

    private class BeforeAsyncSpec : Spec
    {
        public BeforeAsyncSpec()
        {
            describe("Hooks", () =>
            {
                beforeAsync(async () => await Task.CompletedTask);
                it("spec", () => { });
            });
        }
    }

    private class AfterSpec : Spec
    {
        public AfterSpec()
        {
            describe("Hooks", () =>
            {
                after = () => { };
                it("spec", () => { });
            });
        }
    }

    private class AfterAsyncSpec : Spec
    {
        public AfterAsyncSpec()
        {
            describe("Hooks", () =>
            {
                afterAsync(async () => await Task.CompletedTask);
                it("spec", () => { });
            });
        }
    }

    private class NestedDescribeSpec : Spec
    {
        public NestedDescribeSpec()
        {
            describe("outer", () =>
            {
                describe("inner", () =>
                {
                    it("nested spec", () => { });
                });
            });
        }
    }

    private class MultipleDescribeSpec : Spec
    {
        public MultipleDescribeSpec()
        {
            describe("first", () =>
            {
                it("spec1", () => { });
            });

            describe("second", () =>
            {
                it("spec2", () => { });
            });
        }
    }

    private class ExecutableSpec : Spec
    {
        public ExecutableSpec()
        {
            describe("Executable", () =>
            {
                it("passes", () => { });
            });
        }
    }

    #endregion
}

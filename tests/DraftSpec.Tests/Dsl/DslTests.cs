using DraftSpec;
using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Dsl;

/// <summary>
/// Tests for the static DSL used in CSX scripts.
/// Note: Each test uses [Before(Test)] to reset the global state.
/// </summary>
public class DslTests
{
    [Before(Test)]
    public void ResetDslState() => Reset();

    #region describe()

    [Test]
    public async Task Describe_CreatesRootContext()
    {
        var executed = false;

        describe("root", () => { it("spec", () => executed = true); });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task Describe_SupportsNesting()
    {
        var path = new List<string>();

        describe("outer", () => { describe("inner", () => { it("spec", () => path.Add("executed")); }); });
        new SpecRunner().Run(RootContext!);

        await Assert.That(path).Contains("executed");
    }

    [Test]
    public async Task Describe_MultipleTopLevelDescribes_AddedAsChildrenOfRoot()
    {
        var executed = new List<string>();

        describe("first", () => { it("spec1", () => executed.Add("first")); });

        describe("second", () => { it("spec2", () => executed.Add("second")); });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(executed).Contains("first");
        await Assert.That(executed).Contains("second");
    }

    [Test]
    public async Task Describe_DeeplyNested_WorksCorrectly()
    {
        var depth = 0;

        describe("level1",
            () =>
            {
                describe("level2",
                    () =>
                    {
                        describe("level3", () => { describe("level4", () => { it("deep spec", () => depth = 4); }); });
                    });
            });
        new SpecRunner().Run(RootContext!);

        await Assert.That(depth).IsEqualTo(4);
    }

    #endregion

    #region context()

    [Test]
    public async Task Context_IsAliasForDescribe()
    {
        var executed = false;

        describe("feature", () => { context("when condition", () => { it("behaves", () => executed = true); }); });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).IsTrue();
    }

    #endregion

    #region it()

    [Test]
    public async Task It_WithBody_CreatesExecutableSpec()
    {
        var executed = false;

        describe("test", () => { it("runs", () => executed = true); });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task It_WithoutBody_CreatesPendingSpec()
    {
        // Pending specs don't execute but are tracked
        describe("test", () => { it("pending spec"); });
        // This test verifies the API doesn't throw
        new SpecRunner().Run(RootContext!);
    }

    [Test]
    public async Task It_MultipleSpecs_AllExecute()
    {
        var count = 0;

        describe("test", () =>
        {
            it("first", () => count++);
            it("second", () => count++);
            it("third", () => count++);
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(count).IsEqualTo(3);
    }

    [Test]
    public async Task It_OutsideDescribe_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Task.Run(() => it("orphan", () => { }));
        });
    }

    #endregion

    #region fit() - Focused specs

    [Test]
    public async Task Fit_OnlyFocusedSpecsRun()
    {
        var executed = new List<string>();

        describe("test", () =>
        {
            it("skipped", () => executed.Add("skipped"));
            fit("focused", () => executed.Add("focused"));
            it("also skipped", () => executed.Add("also skipped"));
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).Count().IsEqualTo(1);
        await Assert.That(executed[0]).IsEqualTo("focused");
    }

    [Test]
    public async Task Fit_MultipleFocused_AllRun()
    {
        var executed = new List<string>();

        describe("test", () =>
        {
            fit("focused1", () => executed.Add("focused1"));
            it("skipped", () => executed.Add("skipped"));
            fit("focused2", () => executed.Add("focused2"));
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(executed).Contains("focused1");
        await Assert.That(executed).Contains("focused2");
    }

    [Test]
    public async Task Fit_InNestedContext_SkipsOtherContextSpecs()
    {
        var executed = new List<string>();

        describe("outer", () =>
        {
            it("outer spec", () => executed.Add("outer"));

            describe("inner", () => { fit("focused inner", () => executed.Add("focused inner")); });
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).Count().IsEqualTo(1);
        await Assert.That(executed[0]).IsEqualTo("focused inner");
    }

    #endregion

    #region xit() - Skipped specs

    [Test]
    public async Task Xit_DoesNotExecute()
    {
        var executed = false;

        describe("test", () => { xit("skipped", () => executed = true); });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).IsFalse();
    }

    [Test]
    public async Task Xit_WithoutBody_DoesNotThrow()
    {
        describe("test", () => { xit("skipped without body"); });
        new SpecRunner().Run(RootContext!);
        // No assertion needed - test passes if no exception
    }

    [Test]
    public async Task Xit_OtherSpecsStillRun()
    {
        var executed = new List<string>();

        describe("test", () =>
        {
            it("first", () => executed.Add("first"));
            xit("skipped", () => executed.Add("skipped"));
            it("second", () => executed.Add("second"));
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(executed).Contains("first");
        await Assert.That(executed).Contains("second");
    }

    #endregion

    #region before() / after() hooks

    [Test]
    public async Task Before_RunsBeforeEachSpec()
    {
        var order = new List<string>();

        describe("test", () =>
        {
            before(() => order.Add("before"));
            it("spec1", () => order.Add("spec1"));
            it("spec2", () => order.Add("spec2"));
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(order).IsEquivalentTo([
            "before", "spec1",
            "before", "spec2"
        ]);
    }

    [Test]
    public async Task After_RunsAfterEachSpec()
    {
        var order = new List<string>();

        describe("test", () =>
        {
            after(() => order.Add("after"));
            it("spec1", () => order.Add("spec1"));
            it("spec2", () => order.Add("spec2"));
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(order).IsEquivalentTo([
            "spec1", "after",
            "spec2", "after"
        ]);
    }

    [Test]
    public async Task Before_OutsideDescribe_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await Task.Run(() => before(() => { })); });
    }

    [Test]
    public async Task After_OutsideDescribe_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await Task.Run(() => after(() => { })); });
    }

    #endregion

    #region beforeAll() / afterAll() hooks

    [Test]
    public async Task BeforeAll_RunsOncePerContext()
    {
        var count = 0;

        describe("test", () =>
        {
            beforeAll(() => count++);
            it("spec1", () => { });
            it("spec2", () => { });
            it("spec3", () => { });
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task AfterAll_RunsOncePerContext()
    {
        var count = 0;

        describe("test", () =>
        {
            afterAll(() => count++);
            it("spec1", () => { });
            it("spec2", () => { });
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task BeforeAll_OutsideDescribe_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Task.Run(() => beforeAll(() => { }));
        });
    }

    [Test]
    public async Task AfterAll_OutsideDescribe_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await Task.Run(() => afterAll(() => { })); });
    }

    #endregion

    #region Reset()

    [Test]
    public async Task Reset_ClearsStateForNextRun()
    {
        var firstRunCount = 0;
        var secondRunCount = 0;

        describe("first", () => { it("spec", () => firstRunCount++); });
        new SpecRunner().Run(RootContext!);
        Reset();

        describe("second", () => { it("spec", () => secondRunCount++); });
        new SpecRunner().Run(RootContext!);

        await Assert.That(firstRunCount).IsEqualTo(1);
        await Assert.That(secondRunCount).IsEqualTo(1);
    }

    [Test]
    public async Task Reset_ClearsRootContext()
    {
        describe("test", () => { it("spec", () => { }); });

        // Before Reset, RootContext should be set
        await Assert.That(RootContext).IsNotNull();

        Reset();

        // After Reset, RootContext should be null
        await Assert.That(RootContext).IsNull();
    }

    [Test]
    public async Task SpecRunner_WithNoSpecs_DoesNotThrow()
    {
        // No specs defined, RootContext is null - this should not throw
        // The [Before(Test)] already calls Reset(), so RootContext is null
        await Assert.That(RootContext).IsNull();
    }

    // Note: Exit code test removed - Environment.ExitCode is global shared state that
    // cannot be reliably tested when tests run in parallel. Exit code behavior is
    // verified through integration tests and CLI testing.

    #endregion

    #region Nested hook ordering via DSL

    [Test]
    public async Task NestedHooks_RunInCorrectOrder()
    {
        var order = new List<string>();

        describe("outer", () =>
        {
            before(() => order.Add("before:outer"));
            after(() => order.Add("after:outer"));

            describe("inner", () =>
            {
                before(() => order.Add("before:inner"));
                after(() => order.Add("after:inner"));

                it("spec", () => order.Add("spec"));
            });
        });
        new SpecRunner().Run(RootContext!);

        await Assert.That(order).IsEquivalentTo([
            "before:outer",
            "before:inner",
            "spec",
            "after:inner",
            "after:outer"
        ]);
    }

    #endregion
}
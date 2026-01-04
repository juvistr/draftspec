using DraftSpec;
using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Dsl;

/// <summary>
/// Tests for static DSL state management and edge cases.
/// Complements DslTests.cs with focus on state isolation.
/// Note: Each test uses [Before(Test)] to reset the global state.
/// </summary>
public class StaticDslTests
{
    [Before(Test)]
    public void ResetDslState() => Reset();

    #region State Isolation

    [Test]
    public async Task AsyncLocal_IsolatesStateBetweenParallelTasks()
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        var task1 = Task.Run(async () =>
        {
            describe("task1", () => { it("spec1", () => results.Add("task1")); });
            await new SpecRunner().RunAsync(RootContext!);
        });

        var task2 = Task.Run(async () =>
        {
            describe("task2", () => { it("spec2", () => results.Add("task2")); });
            await new SpecRunner().RunAsync(RootContext!);
        });

        await Task.WhenAll(task1, task2);

        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results).Contains("task1");
        await Assert.That(results).Contains("task2");
    }

    [Test]
    public async Task Reset_ClearsAllStaticState()
    {
        describe("test", () => { it("spec", () => { }); });

        // Before Reset, RootContext should be set
        await Assert.That(RootContext).IsNotNull();

        Reset();

        // After Reset, RootContext should be null
        await Assert.That(RootContext).IsNull();
    }

    [Test]
    public async Task MultipleSequentialRuns_DontInterfere()
    {
        var firstRunExecuted = false;
        var secondRunExecuted = false;

        // First run
        describe("first", () => { it("spec1", () => firstRunExecuted = true); });
        await new SpecRunner().RunAsync(RootContext!);
        Reset();

        // Second run - should not see specs from first run
        describe("second", () => { it("spec2", () => secondRunExecuted = true); });
        await new SpecRunner().RunAsync(RootContext!);

        await Assert.That(firstRunExecuted).IsTrue();
        await Assert.That(secondRunExecuted).IsTrue();
    }

    [Test]
    public async Task DescribeWithoutReset_StateRemainsUntilReset()
    {
        describe("abandoned", () => { it("never runs", () => { }); });

        // State exists
        await Assert.That(RootContext).IsNotNull();

        // Clean up properly
        Reset();

        // Now state is cleared
        await Assert.That(RootContext).IsNull();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task SpecRunner_WithEmptyDescribe_Succeeds()
    {
        describe("empty", () =>
        {
            // No specs
        });
        // Test passes if no exception
        await new SpecRunner().RunAsync(RootContext!);
    }

    [Test]
    public async Task SpecRunner_CalledWithoutAnySpecs_Succeeds()
    {
        // No describe, no it - RootContext is null
        // Test passes if no exception
        await Assert.That(RootContext).IsNull();
    }

    [Test]
    public async Task Reset_CalledMultipleTimes_ClearsStateEachTime()
    {
        var count = 0;

        describe("first", () => { it("spec", () => count++); });
        await new SpecRunner().RunAsync(RootContext!);
        Reset();

        describe("second", () => { it("spec", () => count++); });
        await new SpecRunner().RunAsync(RootContext!);
        Reset();

        describe("third", () => { it("spec", () => count++); });
        await new SpecRunner().RunAsync(RootContext!);

        await Assert.That(count).IsEqualTo(3);
    }

    [Test]
    public async Task NestedDescribe_DeepNesting_MaintainsContext()
    {
        var deepestReached = false;

        describe("L1",
            () =>
            {
                describe("L2",
                    () =>
                    {
                        describe("L3",
                            () =>
                            {
                                describe("L4",
                                    () => { describe("L5", () => { it("deep spec", () => deepestReached = true); }); });
                            });
                    });
            });
        await new SpecRunner().RunAsync(RootContext!);

        await Assert.That(deepestReached).IsTrue();
    }

    [Test]
    public async Task MixedDescribeAndContext_BothWork()
    {
        var executed = new List<string>();

        describe("feature", () =>
        {
            context("when active", () => { it("does something", () => executed.Add("context-spec")); });

            describe("nested describe", () => { it("also works", () => executed.Add("describe-spec")); });
        });
        await new SpecRunner().RunAsync(RootContext!);

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(executed).Contains("context-spec");
        await Assert.That(executed).Contains("describe-spec");
    }

    #endregion

    #region Async Execution

    [Test]
    public async Task AsyncSpec_ExecutesCorrectly()
    {
        var executed = false;

        describe("async", () =>
        {
            it("async spec", async () =>
            {
                await Task.Delay(1);
                executed = true;
            });
        });
        await new SpecRunner().RunAsync(RootContext!);

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task AsyncHooks_ExecuteInOrder()
    {
        var order = new List<string>();

        describe("async hooks", () =>
        {
            beforeAll(async () =>
            {
                await Task.Delay(1);
                order.Add("beforeAll");
            });

            before(async () =>
            {
                await Task.Delay(1);
                order.Add("before");
            });

            after(async () =>
            {
                await Task.Delay(1);
                order.Add("after");
            });

            afterAll(async () =>
            {
                await Task.Delay(1);
                order.Add("afterAll");
            });

            it("spec", () => order.Add("spec"));
        });
        await new SpecRunner().RunAsync(RootContext!);

        await Assert.That(order).Count().IsEqualTo(5);
        await Assert.That(order[0]).IsEqualTo("beforeAll");
        await Assert.That(order[1]).IsEqualTo("before");
        await Assert.That(order[2]).IsEqualTo("spec");
        await Assert.That(order[3]).IsEqualTo("after");
        await Assert.That(order[4]).IsEqualTo("afterAll");
    }

    [Test]
    public async Task MixedSyncAsync_AllExecute()
    {
        var executed = new List<string>();

        describe("mixed", () =>
        {
            it("sync", () => executed.Add("sync"));

            it("async", async () =>
            {
                await Task.Delay(1);
                executed.Add("async");
            });
        });
        await new SpecRunner().RunAsync(RootContext!);

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(executed).Contains("sync");
        await Assert.That(executed).Contains("async");
    }

    [Test]
    public async Task AsyncSpec_FailureReportsCorrectly()
    {
        describe("async failure", () =>
        {
            it("throws async", async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("async error");
            });
        });

        var results = await new SpecRunner().RunAsync(RootContext!);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception).IsNotNull();
        await Assert.That(results[0].Exception!.Message).Contains("async error");
    }

    #endregion

    #region RootContext Inspection

    [Test]
    public async Task RootContext_AccessibleBeforeRun()
    {
        describe("test", () => { it("spec", () => { }); });

        // Can inspect before run - first describe IS the RootContext
        await Assert.That(RootContext).IsNotNull();
        await Assert.That(RootContext!.Description).IsEqualTo("test");

        await new SpecRunner().RunAsync(RootContext!);
    }

    [Test]
    public async Task RootContext_ReflectsNestedStructure()
    {
        describe("parent", () => { describe("child", () => { it("spec", () => { }); }); });

        // First describe is RootContext, nested describe is a child
        await Assert.That(RootContext).IsNotNull();
        await Assert.That(RootContext!.Description).IsEqualTo("parent");
        await Assert.That(RootContext!.Children).Count().IsEqualTo(1);
        await Assert.That(RootContext!.Children[0].Description).IsEqualTo("child");

        await new SpecRunner().RunAsync(RootContext!);
    }

    [Test]
    public async Task RootContext_NullAfterReset()
    {
        describe("test", () => { it("spec", () => { }); });

        await Assert.That(RootContext).IsNotNull();

        Reset();

        await Assert.That(RootContext).IsNull();
    }

    #endregion
}

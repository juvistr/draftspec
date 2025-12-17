using DraftSpec.Configuration;
using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Dsl;

/// <summary>
/// Tests for static DSL state management, configuration, and edge cases.
/// Complements DslTests.cs with focus on state isolation and configuration.
/// Note: Each test must call run() to reset the global state.
/// </summary>
public class StaticDslTests
{
    #region State Isolation

    [Test]
    public async Task AsyncLocal_IsolatesStateBetweenParallelTasks()
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        var task1 = Task.Run(() =>
        {
            describe("task1", () =>
            {
                it("spec1", () => results.Add("task1"));
            });
            run();
        });

        var task2 = Task.Run(() =>
        {
            describe("task2", () =>
            {
                it("spec2", () => results.Add("task2"));
            });
            run();
        });

        await Task.WhenAll(task1, task2);

        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results).Contains("task1");
        await Assert.That(results).Contains("task2");
    }

    [Test]
    public async Task Run_ResetsAllStaticState()
    {
        describe("test", () =>
        {
            it("spec", () => { });
        });

        // Before run, RootContext should be set
        await Assert.That(RootContext).IsNotNull();

        run();

        // After run, RootContext should be null
        await Assert.That(RootContext).IsNull();
    }

    [Test]
    public async Task MultipleSequentialRuns_DontInterfere()
    {
        var firstRunExecuted = false;
        var secondRunExecuted = false;

        // First run
        describe("first", () =>
        {
            it("spec1", () => firstRunExecuted = true);
        });
        run();

        // Second run - should not see specs from first run
        describe("second", () =>
        {
            it("spec2", () => secondRunExecuted = true);
        });
        run();

        await Assert.That(firstRunExecuted).IsTrue();
        await Assert.That(secondRunExecuted).IsTrue();
    }

    [Test]
    public async Task DescribeWithoutRun_StateRemainsUntilRun()
    {
        describe("abandoned", () =>
        {
            it("never runs", () => { });
        });

        // State exists
        await Assert.That(RootContext).IsNotNull();

        // Clean up properly
        run();

        // Now state is cleared
        await Assert.That(RootContext).IsNull();
    }

    #endregion

    #region Configuration

    [Test]
    public async Task Configure_WithRunnerBuilder_AppliesMiddleware()
    {
        var specsFiltered = 0;

        // Use filter middleware that allows all specs but tracks calls
        configure(runner => runner.WithFilter(ctx =>
        {
            specsFiltered++;
            return true; // Allow all specs
        }));

        describe("test", () =>
        {
            it("spec1", () => { });
            it("spec2", () => { });
        });
        run();

        // Filter middleware was called for both specs
        await Assert.That(specsFiltered).IsEqualTo(2);
    }

    [Test]
    public async Task Configure_WithDraftSpecConfiguration_SetsConfiguration()
    {
        var configureRan = false;

        configure((DraftSpecConfiguration config) =>
        {
            // Configuration is set up - just verify no error
            configureRan = true;
        });

        describe("test", () =>
        {
            it("spec", () => { });
        });
        run();

        await Assert.That(configureRan).IsTrue();
    }

    [Test]
    public async Task Configure_MultipleCalls_AccumulateCorrectly()
    {
        var filterCount = 0;

        // First configure - add a filter
        configure(runner => runner.WithFilter(ctx =>
        {
            filterCount++;
            return true;
        }));

        // Second configure - add timeout (accumulates)
        configure(runner => runner.WithTimeout(10000));

        describe("test", () =>
        {
            it("spec", () => { });
        });
        run();

        // Filter middleware was called (proving first configure worked)
        await Assert.That(filterCount).IsEqualTo(1);
    }

    [Test]
    public async Task Configure_ResetAfterRun()
    {
        var filterCount = 0;

        configure(runner => runner.WithFilter(ctx =>
        {
            filterCount++;
            return true;
        }));

        describe("first", () =>
        {
            it("spec", () => { });
        });
        run();

        // Second run without configure - middleware should not run
        describe("second", () =>
        {
            it("spec", () => { });
        });
        run();

        // Filter only called during first run (configuration was reset)
        await Assert.That(filterCount).IsEqualTo(1);
    }

    [Test]
    public async Task Configure_WithoutSpecs_NoError()
    {
        configure(runner => runner.WithTimeout(1000));

        // No describe/it calls
        run();

        // Should complete without error
        await Assert.That(true).IsTrue();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Run_WithEmptyDescribe_Succeeds()
    {
        describe("empty", () =>
        {
            // No specs
        });
        run();

        // Should complete without error
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Run_CalledWithoutAnySpecs_Succeeds()
    {
        // No describe, no it, just run
        run();

        // Should complete without error
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Run_CalledMultipleTimes_ResetsEachTime()
    {
        var count = 0;

        describe("first", () =>
        {
            it("spec", () => count++);
        });
        run();

        describe("second", () =>
        {
            it("spec", () => count++);
        });
        run();

        describe("third", () =>
        {
            it("spec", () => count++);
        });
        run();

        await Assert.That(count).IsEqualTo(3);
    }

    [Test]
    public async Task NestedDescribe_DeepNesting_MaintainsContext()
    {
        var deepestReached = false;

        describe("L1", () =>
        {
            describe("L2", () =>
            {
                describe("L3", () =>
                {
                    describe("L4", () =>
                    {
                        describe("L5", () =>
                        {
                            it("deep spec", () => deepestReached = true);
                        });
                    });
                });
            });
        });
        run();

        await Assert.That(deepestReached).IsTrue();
    }

    [Test]
    public async Task MixedDescribeAndContext_BothWork()
    {
        var executed = new List<string>();

        describe("feature", () =>
        {
            context("when active", () =>
            {
                it("does something", () => executed.Add("context-spec"));
            });

            describe("nested describe", () =>
            {
                it("also works", () => executed.Add("describe-spec"));
            });
        });
        run();

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(executed).Contains("context-spec");
        await Assert.That(executed).Contains("describe-spec");
    }

    #endregion

    #region Environment.ExitCode

    [Test]
    public async Task Run_AllPassed_ExitCodeZero()
    {
        // Save original
        var originalExitCode = Environment.ExitCode;

        describe("test", () =>
        {
            it("passes", () => { });
        });
        run();

        var exitCode = Environment.ExitCode;

        // Restore
        Environment.ExitCode = originalExitCode;

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Run_AnyFailed_ExitCodeOne()
    {
        // Save original
        var originalExitCode = Environment.ExitCode;

        describe("test", () =>
        {
            it("fails", () => throw new Exception("intentional"));
        });
        run();

        var exitCode = Environment.ExitCode;

        // Restore
        Environment.ExitCode = originalExitCode;

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Run_OnlyPending_ExitCodeZero()
    {
        // Save original
        var originalExitCode = Environment.ExitCode;

        describe("test", () =>
        {
            it("pending spec"); // No body = pending
        });
        run();

        var exitCode = Environment.ExitCode;

        // Restore
        Environment.ExitCode = originalExitCode;

        await Assert.That(exitCode).IsEqualTo(0);
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
        run();

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
        run();

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
        run();

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(executed).Contains("sync");
        await Assert.That(executed).Contains("async");
    }

    [Test]
    public async Task AsyncSpec_ExceptionPropagates()
    {
        // Save original
        var originalExitCode = Environment.ExitCode;

        describe("async failure", () =>
        {
            it("throws async", async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("async error");
            });
        });
        run();

        var exitCode = Environment.ExitCode;

        // Restore
        Environment.ExitCode = originalExitCode;

        await Assert.That(exitCode).IsEqualTo(1);
    }

    #endregion

    #region RootContext Inspection

    [Test]
    public async Task RootContext_AccessibleBeforeRun()
    {
        describe("test", () =>
        {
            it("spec", () => { });
        });

        // Can inspect before run - first describe IS the RootContext
        await Assert.That(RootContext).IsNotNull();
        await Assert.That(RootContext!.Description).IsEqualTo("test");

        run();
    }

    [Test]
    public async Task RootContext_ReflectsNestedStructure()
    {
        describe("parent", () =>
        {
            describe("child", () =>
            {
                it("spec", () => { });
            });
        });

        // First describe is RootContext, nested describe is a child
        await Assert.That(RootContext).IsNotNull();
        await Assert.That(RootContext!.Description).IsEqualTo("parent");
        await Assert.That(RootContext!.Children).Count().IsEqualTo(1);
        await Assert.That(RootContext!.Children[0].Description).IsEqualTo("child");

        run();
    }

    [Test]
    public async Task RootContext_NullAfterRun()
    {
        describe("test", () =>
        {
            it("spec", () => { });
        });

        await Assert.That(RootContext).IsNotNull();

        run();

        await Assert.That(RootContext).IsNull();
    }

    #endregion
}

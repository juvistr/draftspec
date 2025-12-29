namespace DraftSpec.Tests.Runner;

/// <summary>
/// Tests for --bail functionality that stops execution after first failure.
/// </summary>
public class BailTests
{
    #region Sequential Execution

    [Test]
    public async Task WithBail_StopsAfterFirstFailure_Sequential()
    {
        var executed = new List<string>();
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("passes", () => executed.Add("first")));
        context.AddSpec(new SpecDefinition("fails", () =>
        {
            executed.Add("second");
            throw new Exception("failure");
        }));
        context.AddSpec(new SpecDefinition("never runs", () => executed.Add("third")));

        var runner = SpecRunner.Create().WithBail().Build();
        var results = await runner.RunAsync(context);

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(executed[0]).IsEqualTo("first");
        await Assert.That(executed[1]).IsEqualTo("second");
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[2].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithBail_AllSpecsPassIfNoFailure()
    {
        var executed = new List<string>();
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("first", () => executed.Add("first")));
        context.AddSpec(new SpecDefinition("second", () => executed.Add("second")));
        context.AddSpec(new SpecDefinition("third", () => executed.Add("third")));

        var runner = SpecRunner.Create().WithBail().Build();
        var results = await runner.RunAsync(context);

        await Assert.That(executed).Count().IsEqualTo(3);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
    }

    [Test]
    public async Task WithBail_SkipsNestedContexts()
    {
        var executed = new List<string>();
        var root = new SpecContext("root");
        root.AddSpec(new SpecDefinition("fails", () =>
        {
            executed.Add("root");
            throw new Exception("failure");
        }));

        var child = new SpecContext("child", root);
        child.AddSpec(new SpecDefinition("never runs", () => executed.Add("child")));

        var runner = SpecRunner.Create().WithBail().Build();
        var results = await runner.RunAsync(root);

        await Assert.That(executed).Count().IsEqualTo(1);
        await Assert.That(executed[0]).IsEqualTo("root");
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithBail_SkipsMultipleNestedContexts()
    {
        var executed = new List<string>();
        var root = new SpecContext("root");
        root.AddSpec(new SpecDefinition("fails", () =>
        {
            executed.Add("root");
            throw new Exception("failure");
        }));

        var child1 = new SpecContext("child1", root);
        child1.AddSpec(new SpecDefinition("c1 spec", () => executed.Add("child1")));

        var child2 = new SpecContext("child2", root);
        child2.AddSpec(new SpecDefinition("c2 spec", () => executed.Add("child2")));

        var runner = SpecRunner.Create().WithBail().Build();
        var results = await runner.RunAsync(root);

        await Assert.That(executed).Count().IsEqualTo(1);
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Skipped);
        await Assert.That(results[2].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithoutBail_ContinuesAfterFailure()
    {
        var executed = new List<string>();
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("fails", () =>
        {
            executed.Add("first");
            throw new Exception("failure");
        }));
        context.AddSpec(new SpecDefinition("still runs", () => executed.Add("second")));

        var runner = new SpecRunner(); // No bail
        var results = await runner.RunAsync(context);

        await Assert.That(executed).Count().IsEqualTo(2);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Passed);
    }

    #endregion

    #region Parallel Execution

    [Test]
    public async Task WithBail_WorksWithParallelExecution()
    {
        var executedCount = 0;
        var context = new SpecContext("test");

        // Add 10 specs - first one fails
        context.AddSpec(new SpecDefinition("fails", () =>
        {
            Interlocked.Increment(ref executedCount);
            throw new Exception("failure");
        }));

        for (var i = 1; i < 10; i++)
        {
            context.AddSpec(new SpecDefinition($"spec{i}", () =>
            {
                Thread.Sleep(50); // Give time for cancellation to propagate
                Interlocked.Increment(ref executedCount);
            }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .WithBail()
            .Build();

        var results = await runner.RunAsync(context);

        // Should have at least one failure and some skipped
        await Assert.That(results.Any(r => r.Status == SpecStatus.Failed)).IsTrue();
        await Assert.That(results.Any(r => r.Status == SpecStatus.Skipped)).IsTrue();
        // Not all specs should have executed
        await Assert.That(executedCount).IsLessThan(10);
    }

    [Test]
    public async Task WithBail_ParallelResultsPreserveOrder()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec0", () => { }));
        context.AddSpec(new SpecDefinition("spec1", () => throw new Exception()));
        context.AddSpec(new SpecDefinition("spec2", () => { }));
        context.AddSpec(new SpecDefinition("spec3", () => { }));

        var runner = SpecRunner.Create()
            .WithParallelExecution(2)
            .WithBail()
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(4);
        await Assert.That(results[0].Spec.Description).IsEqualTo("spec0");
        await Assert.That(results[1].Spec.Description).IsEqualTo("spec1");
        await Assert.That(results[2].Spec.Description).IsEqualTo("spec2");
        await Assert.That(results[3].Spec.Description).IsEqualTo("spec3");
    }

    #endregion

    #region Builder API

    [Test]
    public async Task Builder_WithBail_SetsBailFlag()
    {
        var builder = SpecRunner.Create().WithBail();

        await Assert.That(builder.Bail).IsTrue();
    }

    [Test]
    public async Task Builder_Default_BailIsFalse()
    {
        var builder = SpecRunner.Create();

        await Assert.That(builder.Bail).IsFalse();
    }

    #endregion

    #region AfterAll Hook Behavior

    [Test]
    public async Task WithBail_StillRunsAfterAllHooks()
    {
        var afterAllRan = false;
        var context = new SpecContext("test");
        context.AfterAll = () =>
        {
            afterAllRan = true;
            return Task.CompletedTask;
        };
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception()));
        context.AddSpec(new SpecDefinition("skipped", () => { }));

        var runner = SpecRunner.Create().WithBail().Build();
        await runner.RunAsync(context);

        await Assert.That(afterAllRan).IsTrue();
    }

    [Test]
    public async Task WithBail_NestedContext_SkipsContextEntirelyIncludingHooks()
    {
        var childBeforeAllRan = false;
        var root = new SpecContext("root");
        root.AddSpec(new SpecDefinition("fails", () => throw new Exception()));

        var child = new SpecContext("child", root);
        child.BeforeAll = () =>
        {
            childBeforeAllRan = true;
            return Task.CompletedTask;
        };
        child.AddSpec(new SpecDefinition("skipped", () => { }));

        var runner = SpecRunner.Create().WithBail().Build();
        await runner.RunAsync(root);

        // Child's beforeAll should not run since the context is skipped entirely
        await Assert.That(childBeforeAllRan).IsFalse();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task WithBail_FirstSpecFails_AllOthersSkipped()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception()));
        context.AddSpec(new SpecDefinition("skipped1", () => { }));
        context.AddSpec(new SpecDefinition("skipped2", () => { }));

        var runner = SpecRunner.Create().WithBail().Build();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Skipped);
        await Assert.That(results[2].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task WithBail_LastSpecFails_AllOthersPassed()
    {
        var executed = new List<string>();
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("passes1", () => executed.Add("1")));
        context.AddSpec(new SpecDefinition("passes2", () => executed.Add("2")));
        context.AddSpec(new SpecDefinition("fails", () =>
        {
            executed.Add("3");
            throw new Exception();
        }));

        var runner = SpecRunner.Create().WithBail().Build();
        var results = await runner.RunAsync(context);

        await Assert.That(executed).Count().IsEqualTo(3);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(results[2].Status).IsEqualTo(SpecStatus.Failed);
    }

    [Test]
    public async Task WithBail_EmptyContext_NoError()
    {
        var context = new SpecContext("empty");

        var runner = SpecRunner.Create().WithBail().Build();
        var results = await runner.RunAsync(context);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task WithBail_CanRerunAfterBail()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception()));
        context.AddSpec(new SpecDefinition("skipped", () => { }));

        var runner = SpecRunner.Create().WithBail().Build();

        // First run - should bail
        var results1 = await runner.RunAsync(context);
        await Assert.That(results1[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results1[1].Status).IsEqualTo(SpecStatus.Skipped);

        // Second run - should also work (bail state reset)
        var results2 = await runner.RunAsync(context);
        await Assert.That(results2[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results2[1].Status).IsEqualTo(SpecStatus.Skipped);
    }

    #endregion
}

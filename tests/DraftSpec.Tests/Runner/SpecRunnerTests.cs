namespace DraftSpec.Tests.Runner;

/// <summary>
/// Tests for SpecRunner execution.
/// </summary>
public class SpecRunnerTests
{
    #region Basic Execution

    [Test]
    public async Task Run_WithPassingSpec_ReturnsPassedResult()
    {
        var context = new SpecContext("test context");
        context.AddSpec(new SpecDefinition("passes", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task Run_WithFailingSpec_ReturnsFailedResultWithException()
    {
        var context = new SpecContext("test context");
        var expectedException = new InvalidOperationException("test failure");
        context.AddSpec(new SpecDefinition("fails", () => throw expectedException));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception).IsNotNull();
        await Assert.That(results[0].Exception!.Message).IsEqualTo("test failure");
    }

    [Test]
    public async Task Run_WithMultipleSpecs_ExecutesAll()
    {
        var context = new SpecContext("test context");
        var executed = new List<string>();
        context.AddSpec(new SpecDefinition("first", () => executed.Add("first")));
        context.AddSpec(new SpecDefinition("second", () => executed.Add("second")));
        context.AddSpec(new SpecDefinition("third", () => executed.Add("third")));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(executed).Count().IsEqualTo(3);
        await Assert.That(executed[0]).IsEqualTo("first");
        await Assert.That(executed[1]).IsEqualTo("second");
        await Assert.That(executed[2]).IsEqualTo("third");
    }

    #endregion

    #region Duration Tracking

    [Test]
    public async Task Run_TracksDuration()
    {
        var context = new SpecContext("test context");
        context.AddSpec(new SpecDefinition("slow spec", () => Thread.Sleep(10)));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Duration.TotalMilliseconds).IsGreaterThan(5);
    }

    [Test]
    public async Task Run_TracksDurationEvenOnFailure()
    {
        var context = new SpecContext("test context");
        context.AddSpec(new SpecDefinition("failing slow spec", () =>
        {
            Thread.Sleep(10);
            throw new Exception("fail");
        }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Duration.TotalMilliseconds).IsGreaterThan(5);
    }

    #endregion

    #region Pending Specs

    [Test]
    public async Task Run_WithPendingSpec_ReturnsPendingStatus()
    {
        var context = new SpecContext("test context");
        context.AddSpec(new SpecDefinition("pending spec")); // No body = pending

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Pending);
    }

    [Test]
    public async Task Run_WithPendingSpec_DoesNotExecute()
    {
        var executed = false;
        var context = new SpecContext("test context");
        // Pending spec (no body) followed by a regular spec
        context.AddSpec(new SpecDefinition("pending"));
        context.AddSpec(new SpecDefinition("executes", () => executed = true));

        var runner = new SpecRunner();
        await runner.RunAsync(context);

        await Assert.That(executed).IsTrue();
    }

    #endregion

    #region Skipped Specs

    [Test]
    public async Task Run_WithSkippedSpec_ReturnsSkippedStatus()
    {
        var context = new SpecContext("test context");
        context.AddSpec(new SpecDefinition("skipped", () => { }) { IsSkipped = true });

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task Run_WithSkippedSpec_DoesNotExecuteBody()
    {
        var executed = false;
        var context = new SpecContext("test context");
        context.AddSpec(new SpecDefinition("skipped", () => executed = true) { IsSkipped = true });

        var runner = new SpecRunner();
        await runner.RunAsync(context);

        await Assert.That(executed).IsFalse();
    }

    #endregion

    #region Context Path

    [Test]
    public async Task Run_BuildsCorrectContextPath()
    {
        var root = new SpecContext("Calculator");
        var child = new SpecContext("add", root);
        child.AddSpec(new SpecDefinition("returns sum", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        await Assert.That(results[0].ContextPath).Count().IsEqualTo(2);
        await Assert.That(results[0].ContextPath[0]).IsEqualTo("Calculator");
        await Assert.That(results[0].ContextPath[1]).IsEqualTo("add");
    }

    [Test]
    public async Task Run_FullDescriptionIncludesPathAndSpec()
    {
        var root = new SpecContext("Calculator");
        var child = new SpecContext("add", root);
        child.AddSpec(new SpecDefinition("returns sum", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        await Assert.That(results[0].FullDescription).IsEqualTo("Calculator add returns sum");
    }

    [Test]
    public async Task Run_WithDeeplyNestedContexts_BuildsFullPath()
    {
        var level1 = new SpecContext("Level1");
        var level2 = new SpecContext("Level2", level1);
        var level3 = new SpecContext("Level3", level2);
        level3.AddSpec(new SpecDefinition("deep spec", () => { }));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(level1);

        await Assert.That(results[0].ContextPath).Count().IsEqualTo(3);
        await Assert.That(results[0].FullDescription).IsEqualTo("Level1 Level2 Level3 deep spec");
    }

    #endregion

    #region BeforeAll / AfterAll

    [Test]
    public async Task Run_BeforeAll_RunsOnce()
    {
        var beforeAllCount = 0;
        var context = new SpecContext("test");
        context.AddBeforeAll(() =>
        {
            beforeAllCount++;
            return Task.CompletedTask;
        });
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));

        var runner = new SpecRunner();
        await runner.RunAsync(context);

        await Assert.That(beforeAllCount).IsEqualTo(1);
    }

    [Test]
    public async Task Run_AfterAll_RunsOnce()
    {
        var afterAllCount = 0;
        var context = new SpecContext("test");
        context.AddAfterAll(() =>
        {
            afterAllCount++;
            return Task.CompletedTask;
        });
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));

        var runner = new SpecRunner();
        await runner.RunAsync(context);

        await Assert.That(afterAllCount).IsEqualTo(1);
    }

    [Test]
    public async Task Run_AfterAll_RunsEvenIfSpecFails()
    {
        var afterAllRan = false;
        var context = new SpecContext("test");
        context.AddAfterAll(() =>
        {
            afterAllRan = true;
            return Task.CompletedTask;
        });
        context.AddSpec(new SpecDefinition("fails", () => throw new Exception()));

        var runner = new SpecRunner();
        await runner.RunAsync(context);

        await Assert.That(afterAllRan).IsTrue();
    }

    #endregion

    #region Empty Contexts

    [Test]
    public async Task Run_WithEmptyContext_ReturnsEmptyResults()
    {
        var context = new SpecContext("empty");

        var runner = new SpecRunner();
        var results = await runner.RunAsync(context);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task Run_WithNestedEmptyContext_ReturnsEmptyResults()
    {
        var root = new SpecContext("root");
        var _ = new SpecContext("child", root); // Empty child

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        await Assert.That(results).IsEmpty();
    }

    #endregion

    #region Nested Context Execution

    [Test]
    public async Task Run_ExecutesNestedContextSpecs()
    {
        var executed = new List<string>();
        var root = new SpecContext("root");
        root.AddSpec(new SpecDefinition("root spec", () => executed.Add("root")));

        var child = new SpecContext("child", root);
        child.AddSpec(new SpecDefinition("child spec", () => executed.Add("child")));

        var runner = new SpecRunner();
        var results = await runner.RunAsync(root);

        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(executed).Contains("root");
        await Assert.That(executed).Contains("child");
    }

    [Test]
    public async Task Run_ExecutesSpecsInCorrectOrder()
    {
        var order = new List<string>();
        var root = new SpecContext("root");
        root.AddSpec(new SpecDefinition("first", () => order.Add("1")));

        var child = new SpecContext("child", root);
        child.AddSpec(new SpecDefinition("second", () => order.Add("2")));

        root.AddSpec(new SpecDefinition("third", () => order.Add("3")));

        var runner = new SpecRunner();
        await runner.RunAsync(root);

        // Root specs first (in order), then children
        await Assert.That(order[0]).IsEqualTo("1");
        await Assert.That(order[1]).IsEqualTo("3");
        await Assert.That(order[2]).IsEqualTo("2");
    }

    #endregion
}

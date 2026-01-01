using DraftSpec.Execution;

namespace DraftSpec.Tests.Execution;

/// <summary>
/// Tests for SpecRunnerBuilder execution strategy configuration.
/// </summary>
public class SpecRunnerBuilderStrategyTests
{
    [Test]
    public async Task WithExecutionStrategy_UsesProvidedStrategy()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec1", () => { }));

        var customStrategy = new SequentialExecutionStrategy();
        var runner = SpecRunner.Create()
            .WithExecutionStrategy(customStrategy)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task WithExecutionStrategy_OverridesParallelExecution()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));

        // Set parallel first, then override with sequential
        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .WithExecutionStrategy(SequentialExecutionStrategy.Instance)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(2);
    }

    [Test]
    public void WithExecutionStrategy_ThrowsOnNull()
    {
        var builder = SpecRunner.Create();

        Assert.Throws<ArgumentNullException>(() => builder.WithExecutionStrategy(null!));
    }
}

using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

public class InProcessSpecRunnerFactoryTests
{
    [Test]
    public async Task Create_WithNoParameters_ReturnsRunner()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner = factory.Create();

        await Assert.That(runner).IsNotNull();
        await Assert.That(runner).IsAssignableTo<IInProcessSpecRunner>();
    }

    [Test]
    public async Task Create_WithFilterTags_ReturnsRunner()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner = factory.Create(filterTags: "smoke,integration");

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task Create_WithExcludeTags_ReturnsRunner()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner = factory.Create(excludeTags: "slow,flaky");

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task Create_WithFilterName_ReturnsRunner()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner = factory.Create(filterName: "Calculator");

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task Create_WithExcludeName_ReturnsRunner()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner = factory.Create(excludeName: "slow test");

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task Create_WithFilterContext_ReturnsRunner()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner = factory.Create(filterContext: ["Calculator", "add"]);

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task Create_WithExcludeContext_ReturnsRunner()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner = factory.Create(excludeContext: ["slow", "integration"]);

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task Create_WithAllParameters_ReturnsRunner()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner = factory.Create(
            filterTags: "smoke",
            excludeTags: "slow",
            filterName: "Calculator",
            excludeName: "skip",
            filterContext: ["math"],
            excludeContext: ["experimental"]);

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task Create_ReturnsNewInstanceEachTime()
    {
        var factory = new InProcessSpecRunnerFactory();

        var runner1 = factory.Create();
        var runner2 = factory.Create();

        await Assert.That(runner1).IsNotSameReferenceAs(runner2);
    }
}

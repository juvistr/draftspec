using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for CommandFactory class.
/// The factory is a thin wrapper that delegates to ICommandRegistry.
/// </summary>
public class CommandFactoryTests
{
    private MockCommandRegistry _registry = null!;
    private CommandFactory _factory = null!;

    [Before(Test)]
    public void Setup()
    {
        _registry = new MockCommandRegistry();
        _factory = new CommandFactory(_registry);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandFactory(null!));
    }

    #endregion

    #region Delegation Tests

    [Test]
    public async Task Create_DelegatesToRegistry()
    {
        _registry.WithCommand("run");

        _factory.Create("run");

        await Assert.That(_registry.GetExecutorCalls).Contains("run");
    }

    [Test]
    public async Task Create_PassesCommandNameToRegistry()
    {
        _registry.WithCommand("custom-command");

        _factory.Create("custom-command");

        await Assert.That(_registry.GetExecutorCalls).Contains("custom-command");
    }

    [Test]
    public async Task Create_WhenRegistryReturnsExecutor_ReturnsFunction()
    {
        _registry.WithCommand("run");

        var result = _factory.Create("run");

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Create_WhenRegistryReturnsNull_ReturnsNull()
    {
        // Registry has no commands configured

        var result = _factory.Create("unknown");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Create_ReturnedFunction_ExecutesCommand()
    {
        _registry.WithCommand("run", returnCode: 42);

        var executor = _factory.Create("run");
        var result = await executor!(new CliOptions(), CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion
}

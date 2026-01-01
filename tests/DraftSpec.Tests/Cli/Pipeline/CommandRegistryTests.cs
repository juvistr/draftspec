using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline;

/// <summary>
/// Tests for CommandRegistry class.
/// </summary>
public class CommandRegistryTests
{
    private MockConfigApplier _configApplier = null!;
    private CommandRegistry _registry = null!;

    [Before(Test)]
    public void Setup()
    {
        _configApplier = new MockConfigApplier();
        _registry = new CommandRegistry(_configApplier);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_NullConfigApplier_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandRegistry(null!));
    }

    #endregion

    #region Register - Argument Validation

    [Test]
    public void Register_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _registry.Register<TestCommand, TestOptions>(
                null!,
                () => new TestCommand(),
                _ => new TestOptions()));
    }

    [Test]
    public void Register_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _registry.Register<TestCommand, TestOptions>(
                "",
                () => new TestCommand(),
                _ => new TestOptions()));
    }

    [Test]
    public void Register_WhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _registry.Register<TestCommand, TestOptions>(
                "   ",
                () => new TestCommand(),
                _ => new TestOptions()));
    }

    [Test]
    public void Register_NullCommandFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _registry.Register<TestCommand, TestOptions>(
                "test",
                null!,
                _ => new TestOptions()));
    }

    [Test]
    public void Register_NullOptionsConverter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _registry.Register<TestCommand, TestOptions>(
                "test",
                () => new TestCommand(),
                null!));
    }

    #endregion

    #region GetExecutor - Basic Behavior

    [Test]
    public async Task GetExecutor_RegisteredCommand_ReturnsExecutor()
    {
        _registry.Register<TestCommand, TestOptions>(
            "test",
            () => new TestCommand(),
            _ => new TestOptions());

        var executor = _registry.GetExecutor("test");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task GetExecutor_UnregisteredCommand_ReturnsNull()
    {
        var executor = _registry.GetExecutor("unknown");

        await Assert.That(executor).IsNull();
    }

    [Test]
    public async Task GetExecutor_EmptyString_ReturnsNull()
    {
        var executor = _registry.GetExecutor("");

        await Assert.That(executor).IsNull();
    }

    [Test]
    public async Task GetExecutor_Whitespace_ReturnsNull()
    {
        var executor = _registry.GetExecutor("   ");

        await Assert.That(executor).IsNull();
    }

    #endregion

    #region GetExecutor - Case Insensitivity

    [Test]
    [Arguments("TEST")]
    [Arguments("Test")]
    [Arguments("test")]
    [Arguments("tEsT")]
    public async Task GetExecutor_CaseInsensitive(string lookupName)
    {
        _registry.Register<TestCommand, TestOptions>(
            "test",
            () => new TestCommand(),
            _ => new TestOptions());

        var executor = _registry.GetExecutor(lookupName);

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task GetExecutor_RegisteredWithMixedCase_FoundWithLowerCase()
    {
        _registry.Register<TestCommand, TestOptions>(
            "MyCommand",
            () => new TestCommand(),
            _ => new TestOptions());

        var executor = _registry.GetExecutor("mycommand");

        await Assert.That(executor).IsNotNull();
    }

    #endregion

    #region GetExecutor - Multiple Registrations

    [Test]
    public async Task GetExecutor_LastRegistrationWins()
    {
        var command1 = new TestCommand { ReturnCode = 1 };
        var command2 = new TestCommand { ReturnCode = 2 };

        _registry.Register<TestCommand, TestOptions>(
            "test",
            () => command1,
            _ => new TestOptions());

        _registry.Register<TestCommand, TestOptions>(
            "test",
            () => command2,
            _ => new TestOptions());

        var executor = _registry.GetExecutor("test");
        var result = await executor!.ExecuteAsync(new CliOptions(), CancellationToken.None);

        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    public async Task GetExecutor_MultipleCommands_EachReturnsCorrectExecutor()
    {
        var commandA = new TestCommand { ReturnCode = 10 };
        var commandB = new TestCommand { ReturnCode = 20 };

        _registry.Register<TestCommand, TestOptions>(
            "command-a",
            () => commandA,
            _ => new TestOptions());

        _registry.Register<TestCommand, TestOptions>(
            "command-b",
            () => commandB,
            _ => new TestOptions());

        var executorA = _registry.GetExecutor("command-a");
        var executorB = _registry.GetExecutor("command-b");

        var resultA = await executorA!.ExecuteAsync(new CliOptions(), CancellationToken.None);
        var resultB = await executorB!.ExecuteAsync(new CliOptions(), CancellationToken.None);

        await Assert.That(resultA).IsEqualTo(10);
        await Assert.That(resultB).IsEqualTo(20);
    }

    #endregion

    #region GetExecutor - Executor Behavior

    [Test]
    public async Task GetExecutor_ExecutorAppliesConfig()
    {
        _registry.Register<TestCommand, TestOptions>(
            "test",
            () => new TestCommand(),
            _ => new TestOptions());

        var executor = _registry.GetExecutor("test");
        await executor!.ExecuteAsync(new CliOptions(), CancellationToken.None);

        await Assert.That(_configApplier.ApplyConfigCalls).Count().IsEqualTo(1);
    }

    [Test]
    public async Task GetExecutor_ExecutorConvertsOptions()
    {
        var convertedOptions = new TestOptions { Value = "converted" };
        var command = new TestCommand();

        _registry.Register<TestCommand, TestOptions>(
            "test",
            () => command,
            _ => convertedOptions);

        var executor = _registry.GetExecutor("test");
        await executor!.ExecuteAsync(new CliOptions(), CancellationToken.None);

        await Assert.That(command.ReceivedOptions?.Value).IsEqualTo("converted");
    }

    [Test]
    public async Task GetExecutor_ExecutorPassesCliOptionsToConverter()
    {
        CliOptions? capturedOptions = null;

        _registry.Register<TestCommand, TestOptions>(
            "test",
            () => new TestCommand(),
            options =>
            {
                capturedOptions = options;
                return new TestOptions();
            });

        var cliOptions = new CliOptions { Path = "/custom/path" };
        var executor = _registry.GetExecutor("test");
        await executor!.ExecuteAsync(cliOptions, CancellationToken.None);

        await Assert.That(capturedOptions?.Path).IsEqualTo("/custom/path");
    }

    [Test]
    public async Task GetExecutor_EachCallCreatesNewExecutor()
    {
        int factoryCalls = 0;

        _registry.Register<TestCommand, TestOptions>(
            "test",
            () =>
            {
                factoryCalls++;
                return new TestCommand();
            },
            _ => new TestOptions());

        _registry.GetExecutor("test");
        _registry.GetExecutor("test");
        _registry.GetExecutor("test");

        await Assert.That(factoryCalls).IsEqualTo(3);
    }

    #endregion

    #region RegisteredCommands Property

    [Test]
    public async Task RegisteredCommands_Empty_ReturnsEmpty()
    {
        await Assert.That(_registry.RegisteredCommands).IsEmpty();
    }

    [Test]
    public async Task RegisteredCommands_AfterRegistration_ContainsCommandName()
    {
        _registry.Register<TestCommand, TestOptions>(
            "my-command",
            () => new TestCommand(),
            _ => new TestOptions());

        await Assert.That(_registry.RegisteredCommands).Contains("my-command");
    }

    [Test]
    public async Task RegisteredCommands_MultipleRegistrations_ContainsAll()
    {
        _registry.Register<TestCommand, TestOptions>(
            "alpha",
            () => new TestCommand(),
            _ => new TestOptions());

        _registry.Register<TestCommand, TestOptions>(
            "beta",
            () => new TestCommand(),
            _ => new TestOptions());

        await Assert.That(_registry.RegisteredCommands).Contains("alpha");
        await Assert.That(_registry.RegisteredCommands).Contains("beta");
    }

    #endregion

    #region Test Helpers

    private class TestOptions
    {
        public string? Value { get; set; }
    }

    private class TestCommand : ICommand<TestOptions>
    {
        public int ReturnCode { get; set; }
        public TestOptions? ReceivedOptions { get; private set; }

        public Task<int> ExecuteAsync(TestOptions options, CancellationToken cancellationToken)
        {
            ReceivedOptions = options;
            return Task.FromResult(ReturnCode);
        }
    }

    #endregion
}

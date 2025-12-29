using DraftSpec.Cli.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.IntegrationTests.DependencyInjection;

/// <summary>
/// Tests that verify CommandFactory can create all commands.
/// This tests the integration between DI container and command resolution.
/// </summary>
public class CommandFactoryTests
{
    /// <summary>
    /// Verifies CommandFactory can create all known commands.
    /// </summary>
    [Test]
    public async Task CommandFactory_CanCreateAllCommands()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ICommandFactory>();

        var commands = new[] { "run", "watch", "list", "validate", "init", "new", "schema" };

        foreach (var cmd in commands)
        {
            var command = factory.Create(cmd);
            await Assert.That(command)
                .IsNotNull()
                .Because($"Command '{cmd}' must be creatable via factory");
        }
    }

    /// <summary>
    /// Verifies CommandFactory handles case-insensitive command names.
    /// </summary>
    [Test]
    public async Task CommandFactory_IsCaseInsensitive()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ICommandFactory>();

        var variations = new[] { "RUN", "Run", "rUn", "run" };

        foreach (var cmd in variations)
        {
            var command = factory.Create(cmd);
            await Assert.That(command)
                .IsNotNull()
                .Because($"Command '{cmd}' (case variation) should be creatable");
        }
    }

    /// <summary>
    /// Verifies CommandFactory returns null for unknown commands.
    /// </summary>
    [Test]
    public async Task CommandFactory_ReturnsNullForUnknownCommand()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ICommandFactory>();

        var command = factory.Create("nonexistent");

        await Assert.That(command).IsNull()
            .Because("Unknown commands should return null");
    }

    /// <summary>
    /// Verifies each command type is correct.
    /// </summary>
    [Test]
    [Arguments("run", "RunCommand")]
    [Arguments("watch", "WatchCommand")]
    [Arguments("list", "ListCommand")]
    [Arguments("validate", "ValidateCommand")]
    [Arguments("init", "InitCommand")]
    [Arguments("new", "NewCommand")]
    [Arguments("schema", "SchemaCommand")]
    public async Task CommandFactory_ReturnsCorrectCommandType(string commandName, string expectedTypeName)
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ICommandFactory>();

        var command = factory.Create(commandName);

        await Assert.That(command).IsNotNull();
        await Assert.That(command!.GetType().Name).IsEqualTo(expectedTypeName)
            .Because($"'{commandName}' should create {expectedTypeName}");
    }

    /// <summary>
    /// Verifies commands are transient (new instance each time).
    /// </summary>
    [Test]
    public async Task CommandFactory_CreatesNewInstancesEachTime()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ICommandFactory>();

        var run1 = factory.Create("run");
        var run2 = factory.Create("run");

        await Assert.That(ReferenceEquals(run1, run2)).IsFalse()
            .Because("Commands should be transient instances");
    }
}

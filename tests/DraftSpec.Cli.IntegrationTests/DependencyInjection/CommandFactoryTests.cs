using DraftSpec.Cli.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.IntegrationTests.DependencyInjection;

/// <summary>
/// Tests that verify CommandFactory can create command executors.
/// The factory now returns executor functions that wrap command invocation.
/// </summary>
public class CommandFactoryTests
{
    /// <summary>
    /// Verifies CommandFactory can create executors for all known commands.
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
            var executor = factory.Create(cmd);
            await Assert.That(executor)
                .IsNotNull()
                .Because($"Command '{cmd}' must have a creatable executor");
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
            var executor = factory.Create(cmd);
            await Assert.That(executor)
                .IsNotNull()
                .Because($"Command '{cmd}' (case variation) should have a creatable executor");
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

        var executor = factory.Create("nonexistent");

        await Assert.That(executor).IsNull()
            .Because("Unknown commands should return null");
    }

    /// <summary>
    /// Verifies each command executor is creatable.
    /// The factory now returns executor functions, not command instances.
    /// </summary>
    [Test]
    [Arguments("run")]
    [Arguments("watch")]
    [Arguments("list")]
    [Arguments("validate")]
    [Arguments("init")]
    [Arguments("new")]
    [Arguments("schema")]
    public async Task CommandFactory_ReturnsExecutorForCommand(string commandName)
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ICommandFactory>();

        var executor = factory.Create(commandName);

        await Assert.That(executor).IsNotNull()
            .Because($"'{commandName}' should have a creatable executor function");
        // Executors are Func<CliOptions, CancellationToken, Task<int>>
        await Assert.That(executor!.GetType().FullName).Contains("Func")
            .Because("Factory should return an executor function");
    }

    /// <summary>
    /// Verifies executor functions are created fresh each call.
    /// </summary>
    [Test]
    public async Task CommandFactory_CreatesNewExecutorsEachTime()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ICommandFactory>();

        var run1 = factory.Create("run");
        var run2 = factory.Create("run");

        await Assert.That(ReferenceEquals(run1, run2)).IsFalse()
            .Because("Executor functions should be new instances each time");
    }
}

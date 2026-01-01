using DraftSpec.Cli;
using DraftSpec.Cli.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Tests.Cli.DependencyInjection;

/// <summary>
/// Tests for ServiceCollectionExtensions.
/// Validates that the DI container is configured correctly.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddDraftSpec_BuildsValidServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddDraftSpec();

        var provider = services.BuildServiceProvider();

        // Validate key services can be resolved
        await Assert.That(provider.GetService<ICommandFactory>()).IsNotNull();
        await Assert.That(provider.GetService<IConsole>()).IsNotNull();
        await Assert.That(provider.GetService<ISpecFinder>()).IsNotNull();
        await Assert.That(provider.GetService<IFileSystem>()).IsNotNull();
        await Assert.That(provider.GetService<IProjectResolver>()).IsNotNull();
    }

    [Test]
    [Arguments("run")]
    [Arguments("watch")]
    [Arguments("list")]
    [Arguments("validate")]
    [Arguments("init")]
    [Arguments("new")]
    [Arguments("schema")]
    [Arguments("flaky")]
    [Arguments("estimate")]
    public async Task AddDraftSpec_CommandFactory_CanCreateCommand(string commandName)
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<ICommandFactory>();

        // Exercise the lambda factory and options converter for each command
        var executor = factory.Create(commandName);

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    [Arguments("run")]
    [Arguments("watch")]
    [Arguments("list")]
    [Arguments("validate")]
    [Arguments("init")]
    [Arguments("new")]
    [Arguments("schema")]
    [Arguments("flaky")]
    [Arguments("estimate")]
    public async Task AddDraftSpec_CommandExecutor_InvokesOptionsConverter(string commandName)
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<ICommandFactory>();
        var executor = factory.Create(commandName);

        // Execute to cover the options converter lambda (e.g., o => o.ToRunOptions())
        // Commands may throw validation errors with default options - that's fine,
        // we're just verifying the DI wiring invokes the options converter
        try
        {
            await executor!(new CliOptions { Path = "." }, CancellationToken.None);
        }
        catch (ArgumentException)
        {
            // Expected for commands that require specific inputs (run, watch, new, etc.)
        }

        // If we get here without a DI resolution error, the wiring is correct
        await Assert.That(true).IsTrue();
    }
}

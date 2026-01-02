using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.IntegrationTests.DependencyInjection;

/// <summary>
/// Tests that verify all DI-registered services can be resolved.
/// These tests would have caught the PluginLoader DI bug where params string[]
/// couldn't be resolved by the DI container.
/// </summary>
public class ServiceResolutionTests
{
    /// <summary>
    /// Verifies all infrastructure services can be resolved from the container.
    /// This is the critical test that catches DI registration bugs.
    /// </summary>
    [Test]
    public async Task AllInfrastructureServices_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        // Infrastructure - Core services
        var coreTypes = new Type[]
        {
            typeof(IConsole),
            typeof(IFileSystem),
            typeof(IEnvironment),
            typeof(DraftSpec.IClock), // Explicit namespace to avoid System.TimeProvider ambiguity
            typeof(IProcessRunner),
        };

        foreach (var type in coreTypes)
        {
            var service = provider.GetService(type);
            await Assert.That(service)
                .IsNotNull()
                .Because($"Core service {type.Name} must be resolvable");
        }
    }

    /// <summary>
    /// Verifies all build-related services can be resolved.
    /// </summary>
    [Test]
    public async Task AllBuildServices_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var buildTypes = new Type[]
        {
            typeof(IBuildCache),
            typeof(IProjectBuilder),
            typeof(ISpecScriptExecutor),
        };

        foreach (var type in buildTypes)
        {
            var service = provider.GetService(type);
            await Assert.That(service)
                .IsNotNull()
                .Because($"Build service {type.Name} must be resolvable");
        }
    }

    /// <summary>
    /// Verifies all application services can be resolved.
    /// </summary>
    [Test]
    public async Task AllApplicationServices_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var appTypes = new Type[]
        {
            typeof(IProjectResolver),
            typeof(IConfigLoader),
            typeof(ISpecFinder),
            typeof(ICliFormatterRegistry),
            typeof(IInProcessSpecRunnerFactory),
            typeof(IFileWatcherFactory),
            typeof(IGitService),
        };

        foreach (var type in appTypes)
        {
            var service = provider.GetService(type);
            await Assert.That(service)
                .IsNotNull()
                .Because($"Application service {type.Name} must be resolvable");
        }
    }

    /// <summary>
    /// Verifies all plugin-related services can be resolved.
    /// This specifically tests the services that had the PluginLoader DI bug.
    /// </summary>
    [Test]
    public async Task AllPluginServices_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var pluginTypes = new Type[]
        {
            typeof(IPluginScanner),
            typeof(IAssemblyLoader),
            typeof(IPluginLoader),
        };

        foreach (var type in pluginTypes)
        {
            var service = provider.GetService(type);
            await Assert.That(service)
                .IsNotNull()
                .Because($"Plugin service {type.Name} must be resolvable - this was the PluginLoader bug");
        }
    }

    /// <summary>
    /// Verifies all command types can be resolved.
    /// </summary>
    [Test]
    public async Task AllCommands_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var commandTypes = new Type[]
        {
            typeof(RunCommand),
            typeof(WatchCommand),
            typeof(ListCommand),
            typeof(ValidateCommand),
            typeof(InitCommand),
            typeof(NewCommand),
            typeof(SchemaCommand),
        };

        foreach (var type in commandTypes)
        {
            var service = provider.GetService(type);
            await Assert.That(service)
                .IsNotNull()
                .Because($"Command {type.Name} must be resolvable");
        }
    }

    /// <summary>
    /// Verifies the command factory can be resolved.
    /// </summary>
    [Test]
    public async Task CommandFactory_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var factory = provider.GetService<ICommandFactory>();

        await Assert.That(factory)
            .IsNotNull()
            .Because("ICommandFactory must be resolvable");
    }

    /// <summary>
    /// Comprehensive test: All registered services can be resolved in a single pass.
    /// This is the gold standard test for catching DI bugs.
    /// </summary>
    [Test]
    public async Task AllRegisteredServices_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        // Complete list from ServiceCollectionExtensions.AddDraftSpec()
        var allTypes = new Type[]
        {
            // Infrastructure - Core
            typeof(IConsole),
            typeof(IFileSystem),
            typeof(IEnvironment),
            typeof(DraftSpec.IClock), // Explicit namespace to avoid System.TimeProvider ambiguity
            typeof(IProcessRunner),

            // Infrastructure - Build
            typeof(IBuildCache),
            typeof(IProjectBuilder),
            typeof(ISpecScriptExecutor),

            // Infrastructure - Services
            typeof(IProjectResolver),
            typeof(IConfigLoader),
            typeof(ISpecFinder),
            typeof(ICliFormatterRegistry),
            typeof(IInProcessSpecRunnerFactory),
            typeof(IFileWatcherFactory),
            typeof(IPluginScanner),
            typeof(IAssemblyLoader),
            typeof(IPluginLoader),
            typeof(IGitService),

            // Commands
            typeof(RunCommand),
            typeof(WatchCommand),
            typeof(ListCommand),
            typeof(ValidateCommand),
            typeof(InitCommand),
            typeof(NewCommand),
            typeof(SchemaCommand),

            // Factory
            typeof(ICommandFactory),
        };

        var failures = new List<string>();

        foreach (var type in allTypes)
        {
            try
            {
                var service = provider.GetService(type);
                if (service == null)
                {
                    failures.Add($"{type.Name}: returned null");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{type.Name}: {ex.Message}");
            }
        }

        await Assert.That(failures)
            .IsEmpty()
            .Because($"All services must be resolvable. Failures:\n{string.Join("\n", failures)}");
    }

    /// <summary>
    /// Verifies that singleton services return the same instance.
    /// </summary>
    [Test]
    public async Task SingletonServices_ReturnSameInstance()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();

        // These should all be singletons
        var console1 = provider.GetRequiredService<IConsole>();
        var console2 = provider.GetRequiredService<IConsole>();

        var fileSystem1 = provider.GetRequiredService<IFileSystem>();
        var fileSystem2 = provider.GetRequiredService<IFileSystem>();

        await Assert.That(ReferenceEquals(console1, console2)).IsTrue()
            .Because("IConsole should be a singleton");
        await Assert.That(ReferenceEquals(fileSystem1, fileSystem2)).IsTrue()
            .Because("IFileSystem should be a singleton");
    }

    /// <summary>
    /// Verifies that transient command services return new instances.
    /// </summary>
    [Test]
    public async Task TransientCommands_ReturnNewInstances()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        using var provider = services.BuildServiceProvider();

        var run1 = provider.GetRequiredService<RunCommand>();
        var run2 = provider.GetRequiredService<RunCommand>();

        await Assert.That(ReferenceEquals(run1, run2)).IsFalse()
            .Because("Commands should be transient (new instance each time)");
    }
}

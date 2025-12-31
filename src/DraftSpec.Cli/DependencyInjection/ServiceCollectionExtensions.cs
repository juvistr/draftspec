using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Services;
using DraftSpec.Cli.Watch;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Extension methods for registering DraftSpec services with DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all DraftSpec CLI services including formatters, finders, and runners.
    /// </summary>
    public static IServiceCollection AddDraftSpec(this IServiceCollection services)
    {
        // Infrastructure - Core
        services.AddSingleton<IConsole, SystemConsole>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironment, SystemEnvironment>();
        services.AddSingleton<DraftSpec.IClock, DraftSpec.SystemClock>();
        services.AddSingleton<IProcessRunner, SystemProcessRunner>();
        services.AddSingleton<IUsageWriter, UsageWriter>();

        // Infrastructure - Build
        services.AddSingleton<IBuildCache, InMemoryBuildCache>();
        services.AddSingleton<IProjectBuilder, DotnetProjectBuilder>();
        services.AddSingleton<ISpecScriptExecutor, RoslynSpecScriptExecutor>();

        // Infrastructure - Services
        services.AddSingleton<IProjectResolver, ProjectResolver>();
        services.AddSingleton<IConfigLoader, ConfigLoader>();
        services.AddSingleton<ISpecFinder, SpecFinder>();
        services.AddSingleton<ICliFormatterRegistry, CliFormatterRegistry>();
        services.AddSingleton<IInProcessSpecRunnerFactory, InProcessSpecRunnerFactory>();
        services.AddSingleton<IFileWatcherFactory, FileWatcherFactory>();
        services.AddSingleton<IPluginScanner, SystemPluginScanner>();
        services.AddSingleton<IAssemblyLoader, IsolatedAssemblyLoader>();
        services.AddSingleton<IPluginLoader, PluginLoader>();
        services.AddSingleton<ISpecStatsCollector, SpecStatsCollector>();
        services.AddSingleton<ISpecPartitioner, SpecPartitioner>();
        services.AddSingleton<ISpecChangeTracker, SpecChangeTracker>();
        services.AddSingleton<IGitService, GitService>();

        // Commands
        services.AddTransient<RunCommand>();
        services.AddTransient<WatchCommand>();
        services.AddTransient<ListCommand>();
        services.AddTransient<ValidateCommand>();
        services.AddTransient<InitCommand>();
        services.AddTransient<NewCommand>();
        services.AddTransient<SchemaCommand>();

        // Pipeline
        services.AddSingleton<IConfigApplier, ConfigApplier>();

        // Factory with explicit command factories for testability
        services.AddSingleton<ICommandFactory>(sp => new CommandFactory(
            sp.GetRequiredService<IConfigApplier>(),
            () => sp.GetRequiredService<RunCommand>(),
            () => sp.GetRequiredService<WatchCommand>(),
            () => sp.GetRequiredService<ListCommand>(),
            () => sp.GetRequiredService<ValidateCommand>(),
            () => sp.GetRequiredService<InitCommand>(),
            () => sp.GetRequiredService<NewCommand>(),
            () => sp.GetRequiredService<SchemaCommand>()));

        return services;
    }
}

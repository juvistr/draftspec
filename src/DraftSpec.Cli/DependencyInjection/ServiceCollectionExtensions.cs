using DraftSpec.Abstractions;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.History;
using DraftSpec.Cli.Interactive;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Common;
using DraftSpec.Cli.Pipeline.Phases.Docs;
using DraftSpec.Cli.Pipeline.Phases.List;
using DraftSpec.Cli.Pipeline.Phases.Validate;
using DraftSpec.Cli.Pipeline.Phases.CoverageMap;
using DraftSpec.Cli.Pipeline.Phases.Init;
using DraftSpec.Cli.Pipeline.Phases.NewSpec;
using DraftSpec.Cli.Pipeline.Phases.Schema;
using DraftSpec.Cli.Pipeline.Phases.Cache;
using DraftSpec.Cli.Pipeline.Phases.History;
using DraftSpec.Cli.Pipeline.Phases.Estimate;
using DraftSpec.Cli.Pipeline.Phases.Flaky;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Cli.DependencyGraph;
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
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IProcessRunner, SystemProcessRunner>();
        services.AddSingleton<IUsageWriter, UsageWriter>();
        services.AddSingleton<IOperatingSystem, SystemOperatingSystem>();
        services.AddSingleton<IPathComparer, SystemPathComparer>();
        services.AddSingleton<IPathValidator, PathValidator>();

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
        services.AddSingleton<IWatchEventProcessor, WatchEventProcessor>();
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<ISpecHistoryService, SpecHistoryService>();
        services.AddSingleton<IRuntimeEstimator, RuntimeEstimator>();
        services.AddSingleton<ISpecSelector, InteractiveSpecSelector>();
        services.AddSingleton<IStaticSpecParserFactory, StaticSpecParserFactory>();
        services.AddSingleton<ICoverageMapService, CoverageMapService>();
        services.AddSingleton<IDependencyGraphBuilder, DependencyGraphBuilder>();

        // Pipeline Phases - Common
        services.AddSingleton<PathResolutionPhase>();
        services.AddSingleton<ProjectDiscoveryPhase>();
        services.AddSingleton<SpecDiscoveryPhase>();
        services.AddSingleton<SpecParsingPhase>();

        // Pipeline Phases - List
        services.AddSingleton<FilterApplyPhase>();
        services.AddSingleton<ListOutputPhase>();

        // Pipeline Phases - Docs
        services.AddSingleton<DocsOutputPhase>();

        // Pipeline Phases - Validate
        services.AddSingleton<ValidationPhase>();
        services.AddSingleton<ValidateOutputPhase>();

        // Pipeline Phases - CoverageMap
        services.AddSingleton<SourceDiscoveryPhase>();
        services.AddSingleton<CoverageMapPhase>();
        services.AddSingleton<CoverageMapOutputPhase>();

        // Pipeline Phases - Init
        services.AddSingleton<InitOutputPhase>();

        // Pipeline Phases - New
        services.AddSingleton<NewSpecOutputPhase>();

        // Pipeline Phases - Schema
        services.AddSingleton<SchemaOutputPhase>();

        // Pipeline Phases - Cache
        services.AddSingleton<CacheOperationPhase>();

        // Pipeline Phases - History (shared)
        services.AddSingleton<HistoryLoadPhase>();

        // Pipeline Phases - Estimate
        services.AddSingleton<EstimateOutputPhase>();

        // Pipeline Phases - Flaky
        services.AddSingleton<FlakyOutputPhase>();

        // Pipeline Phases - Run
        services.AddSingleton<QuarantinePhase>();
        services.AddSingleton<LineFilterPhase>();
        services.AddSingleton<ImpactAnalysisPhase>();
        services.AddSingleton<InteractiveSelectionPhase>();
        services.AddSingleton<PartitionPhase>();
        services.AddSingleton<PreRunStatsPhase>();
        services.AddSingleton<SpecExecutionPhase>();
        services.AddSingleton<HistoryRecordPhase>();
        services.AddSingleton<RunOutputPhase>();

        // Keyed Pipelines - Singleton since phases are singletons and the delegate is stateless
        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "list",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<SpecDiscoveryPhase>())
                .Use(sp.GetRequiredService<SpecParsingPhase>())
                .Use(sp.GetRequiredService<FilterApplyPhase>())
                .Use(sp.GetRequiredService<ListOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "docs",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<SpecDiscoveryPhase>())
                .Use(sp.GetRequiredService<SpecParsingPhase>())
                .Use(sp.GetRequiredService<FilterApplyPhase>())
                .Use(sp.GetRequiredService<DocsOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "validate",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<SpecDiscoveryPhase>())
                .Use(sp.GetRequiredService<ValidationPhase>())
                .Use(sp.GetRequiredService<ValidateOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "coverage-map",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<SourceDiscoveryPhase>())
                .Use(sp.GetRequiredService<CoverageMapPhase>())
                .Use(sp.GetRequiredService<CoverageMapOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "init",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<ProjectDiscoveryPhase>())
                .Use(sp.GetRequiredService<InitOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "new",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<NewSpecOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "schema",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<SchemaOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "cache",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<CacheOperationPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "estimate",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<HistoryLoadPhase>())
                .Use(sp.GetRequiredService<EstimateOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "flaky",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<HistoryLoadPhase>())
                .Use(sp.GetRequiredService<FlakyOutputPhase>())
                .Build());

        services.AddKeyedSingleton<Func<CommandContext, CancellationToken, Task<int>>>(
            "run",
            (sp, _) => new CommandPipelineBuilder()
                .Use(sp.GetRequiredService<PathResolutionPhase>())
                .Use(sp.GetRequiredService<QuarantinePhase>())
                .Use(sp.GetRequiredService<SpecDiscoveryPhase>())
                .Use(sp.GetRequiredService<LineFilterPhase>())
                .Use(sp.GetRequiredService<ImpactAnalysisPhase>())
                .Use(sp.GetRequiredService<InteractiveSelectionPhase>())
                .Use(sp.GetRequiredService<PartitionPhase>())
                .Use(sp.GetRequiredService<PreRunStatsPhase>())
                .Use(sp.GetRequiredService<SpecExecutionPhase>())
                .Use(sp.GetRequiredService<RunOutputPhase>())
                .Use(sp.GetRequiredService<HistoryRecordPhase>())
                .Build());

        // Commands
        services.AddTransient<RunCommand>();
        services.AddTransient<WatchCommand>();
        services.AddTransient<ListCommand>();
        services.AddTransient<ValidateCommand>();
        services.AddTransient<InitCommand>();
        services.AddTransient<NewCommand>();
        services.AddTransient<SchemaCommand>();
        services.AddTransient<FlakyCommand>();
        services.AddTransient<EstimateCommand>();
        services.AddTransient<CacheCommand>();
        services.AddTransient<DocsCommand>();
        services.AddTransient<CoverageMapCommand>();

        // Pipeline
        services.AddSingleton<IConfigApplier, ConfigApplier>();

        // Command Registry - registers all commands in one place
        services.AddSingleton<ICommandRegistry>(sp =>
        {
            var registry = new CommandRegistry(sp.GetRequiredService<IConfigApplier>());

            registry.Register<RunCommand, RunOptions>(
                "run",
                () => sp.GetRequiredService<RunCommand>(),
                o => o.ToRunOptions());

            registry.Register<WatchCommand, WatchOptions>(
                "watch",
                () => sp.GetRequiredService<WatchCommand>(),
                o => o.ToWatchOptions());

            registry.Register<ListCommand, ListOptions>(
                "list",
                () => sp.GetRequiredService<ListCommand>(),
                o => o.ToListOptions());

            registry.Register<ValidateCommand, ValidateOptions>(
                "validate",
                () => sp.GetRequiredService<ValidateCommand>(),
                o => o.ToValidateOptions());

            registry.Register<InitCommand, InitOptions>(
                "init",
                () => sp.GetRequiredService<InitCommand>(),
                o => o.ToInitOptions());

            registry.Register<NewCommand, NewOptions>(
                "new",
                () => sp.GetRequiredService<NewCommand>(),
                o => o.ToNewOptions());

            registry.Register<SchemaCommand, SchemaOptions>(
                "schema",
                () => sp.GetRequiredService<SchemaCommand>(),
                o => o.ToSchemaOptions());

            registry.Register<FlakyCommand, FlakyOptions>(
                "flaky",
                () => sp.GetRequiredService<FlakyCommand>(),
                o => o.ToFlakyOptions());

            registry.Register<EstimateCommand, EstimateOptions>(
                "estimate",
                () => sp.GetRequiredService<EstimateCommand>(),
                o => o.ToEstimateOptions());

            registry.Register<CacheCommand, CacheOptions>(
                "cache",
                () => sp.GetRequiredService<CacheCommand>(),
                o => o.ToCacheOptions());

            registry.Register<DocsCommand, DocsOptions>(
                "docs",
                () => sp.GetRequiredService<DocsCommand>(),
                o => o.ToDocsOptions());

            registry.Register<CoverageMapCommand, CoverageMapOptions>(
                "coverage-map",
                () => sp.GetRequiredService<CoverageMapCommand>(),
                o => o.ToCoverageMapOptions());

            return registry;
        });

        // Factory delegates to registry
        services.AddSingleton<ICommandFactory, CommandFactory>();

        return services;
    }
}

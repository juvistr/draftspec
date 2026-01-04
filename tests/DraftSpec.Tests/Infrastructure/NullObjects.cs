using DraftSpec.Cli;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.History;
using DraftSpec.Cli.Interactive;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Services;
using DraftSpec.Cli.Watch;
using DraftSpec.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure;

/// <summary>
/// Static accessors for null-object test doubles.
/// These do-nothing implementations are useful when a dependency
/// is required but its behavior is not relevant to the test.
/// </summary>
public static class NullObjects
{
    /// <summary>
    /// A no-op console that discards all output.
    /// </summary>
    public static IConsole Console { get; } = new NullConsole();

    /// <summary>
    /// A no-op file system that reports no files exist.
    /// </summary>
    public static IFileSystem FileSystem { get; } = new NullFileSystem();

    /// <summary>
    /// A no-op spec finder that returns empty results.
    /// </summary>
    public static ISpecFinder SpecFinder { get; } = new NullSpecFinder();

    /// <summary>
    /// A no-op runner factory that creates null runners.
    /// </summary>
    public static IInProcessSpecRunnerFactory RunnerFactory { get; } = new NullRunnerFactory();

    /// <summary>
    /// A no-op spec runner.
    /// </summary>
    public static IInProcessSpecRunner Runner { get; } = new NullRunner();

    /// <summary>
    /// A no-op formatter registry.
    /// </summary>
    public static ICliFormatterRegistry FormatterRegistry { get; } = new NullFormatterRegistry();

    /// <summary>
    /// A config loader that returns empty config.
    /// </summary>
    public static IConfigLoader ConfigLoader { get; } = new NullConfigLoader();

    /// <summary>
    /// A no-op config applier.
    /// </summary>
    public static IConfigApplier ConfigApplier { get; } = new NullConfigApplier();

    /// <summary>
    /// A no-op environment.
    /// </summary>
    public static IEnvironment Environment { get; } = new NullEnvironment();

    /// <summary>
    /// A no-op stats collector.
    /// </summary>
    public static ISpecStatsCollector StatsCollector { get; } = new NullStatsCollector();

    /// <summary>
    /// A no-op partitioner.
    /// </summary>
    public static ISpecPartitioner Partitioner { get; } = new NullPartitioner();

    /// <summary>
    /// A no-op file watcher factory.
    /// </summary>
    public static IFileWatcherFactory FileWatcherFactory { get; } = new NullFileWatcherFactory();

    /// <summary>
    /// A no-op project resolver.
    /// </summary>
    public static IProjectResolver ProjectResolver { get; } = new NullProjectResolver();

    /// <summary>
    /// A no-op spec change tracker.
    /// </summary>
    public static ISpecChangeTracker SpecChangeTracker { get; } = new NullSpecChangeTracker();

    /// <summary>
    /// A no-op git service.
    /// </summary>
    public static IGitService GitService { get; } = new NullGitService();

    /// <summary>
    /// A no-op history service.
    /// </summary>
    public static ISpecHistoryService HistoryService { get; } = new NullHistoryService();

    /// <summary>
    /// A no-op runtime estimator.
    /// </summary>
    public static IRuntimeEstimator RuntimeEstimator { get; } = new NullRuntimeEstimator();

    /// <summary>
    /// A no-op spec selector.
    /// </summary>
    public static ISpecSelector SpecSelector { get; } = new NullSpecSelector();

    /// <summary>
    /// A no-op watch event processor that always returns RunAll.
    /// </summary>
    public static IWatchEventProcessor WatchEventProcessor { get; } = new NullWatchEventProcessor();

    #region Null Object Implementations

    private class NullConsole : IConsole
    {
        public void Write(string text) { }
        public void WriteLine(string text) { }
        public void WriteLine() { }
        public ConsoleColor ForegroundColor { get; set; }
        public void ResetColor() { }
        public void Clear() { }
        public void WriteWarning(string text) { }
        public void WriteSuccess(string text) { }
        public void WriteError(string text) { }
    }

    private class NullFileSystem : IFileSystem
    {
        public bool FileExists(string path) => false;
        public void WriteAllText(string path, string content) { }
        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default) => Task.CompletedTask;
        public string ReadAllText(string path) => "";
        public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default) => Task.FromResult("");
        public bool DirectoryExists(string path) => true;
        public void CreateDirectory(string path) { }
        public string[] GetFiles(string path, string searchPattern) => [];
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [];
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => [];
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => [];
        public DateTime GetLastWriteTimeUtc(string path) => DateTime.MinValue;
        public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false) { }
        public void DeleteFile(string path) { }
    }

    private class NullSpecFinder : ISpecFinder
    {
        public IReadOnlyList<string> FindSpecs(string path, string? baseDirectory = null) => [];
    }

    private class NullRunnerFactory : IInProcessSpecRunnerFactory
    {
        public IInProcessSpecRunner Create(
            string? filterTags = null,
            string? excludeTags = null,
            string? filterName = null,
            string? excludeName = null,
            IReadOnlyList<string>? filterContext = null,
            IReadOnlyList<string>? excludeContext = null) => new NullRunner();
    }

    private class NullRunner : IInProcessSpecRunner
    {
#pragma warning disable CS0067
        public event Action<string>? OnBuildStarted;
        public event Action<BuildResult>? OnBuildCompleted;
        public event Action<string>? OnBuildSkipped;
#pragma warning restore CS0067

        public Task<InProcessRunResult> RunFileAsync(string specFile, CancellationToken ct = default)
            => Task.FromResult(new InProcessRunResult(
                specFile,
                new SpecReport(),
                TimeSpan.Zero,
                null));

        public Task<InProcessRunSummary> RunAllAsync(
            IReadOnlyList<string> specFiles,
            bool parallel = false,
            CancellationToken ct = default)
            => Task.FromResult(new InProcessRunSummary([], TimeSpan.Zero));

        public void ClearBuildCache() { }
    }

    private class NullFormatterRegistry : ICliFormatterRegistry
    {
        public IFormatter? GetFormatter(string name, string? cssUrl = null) => null;
        public void Register(string name, Func<string?, IFormatter> factory) { }
        public IEnumerable<string> Names => [];
    }

    private class NullConfigLoader : IConfigLoader
    {
        public ConfigLoadResult Load(string? path = null) => new(null, null, null);
    }

    private class NullConfigApplier : IConfigApplier
    {
        public void ApplyConfig(CliOptions options) { }
    }

    private class NullEnvironment : IEnvironment
    {
        public string CurrentDirectory => Directory.GetCurrentDirectory();
        public string NewLine => System.Environment.NewLine;
    }

    private class NullStatsCollector : ISpecStatsCollector
    {
        public Task<SpecStats> CollectAsync(
            IReadOnlyList<string> specFiles,
            string projectPath,
            CancellationToken ct = default)
        {
            return Task.FromResult(new SpecStats(
                Total: 0,
                Regular: 0,
                Focused: 0,
                Skipped: 0,
                Pending: 0,
                HasFocusMode: false,
                FileCount: specFiles.Count));
        }
    }

    private class NullPartitioner : ISpecPartitioner
    {
        public Task<PartitionResult> PartitionAsync(
            IReadOnlyList<string> specFiles,
            int totalPartitions,
            int partitionIndex,
            PartitionStrategy strategy,
            string projectPath,
            CancellationToken ct = default)
        {
            return Task.FromResult(new PartitionResult(specFiles, specFiles.Count));
        }
    }

    private class NullFileWatcherFactory : IFileWatcherFactory
    {
        public IFileWatcher Create(string path, int debounceMs = 200) => new NullFileWatcher();
    }

    private class NullFileWatcher : IFileWatcher
    {
        public IAsyncEnumerable<FileChangeInfo> WatchAsync(CancellationToken ct = default)
            => AsyncEnumerable.Empty<FileChangeInfo>();

        public void Dispose() { }
    }

    private class NullProjectResolver : IProjectResolver
    {
        public string? FindProject(string directory) => null;
        public ProjectInfo? GetProjectInfo(string csprojPath) => null;
    }

    private class NullSpecChangeTracker : ISpecChangeTracker
    {
        public void RecordState(string filePath, StaticParseResult parseResult) { }

        public SpecChangeSet GetChanges(string filePath, StaticParseResult newResult, bool dependencyChanged)
        {
            return new SpecChangeSet(filePath, [], HasDynamicSpecs: false, DependencyChanged: false);
        }

        public bool HasState(string filePath) => false;
        public void Clear() { }
        public void RecordDependency(string dependencyPath, DateTime lastModified) { }
        public bool HasDependencyChanged(string dependencyPath, DateTime currentModified) => false;
    }

    private class NullGitService : IGitService
    {
        public Task<IReadOnlyList<string>> GetChangedFilesAsync(
            string reference,
            string workingDirectory,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<bool> IsGitRepositoryAsync(
            string directory,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private class NullHistoryService : ISpecHistoryService
    {
        public Task<SpecHistory> LoadAsync(string projectPath, CancellationToken ct = default)
            => Task.FromResult(SpecHistory.Empty);

        public Task SaveAsync(string projectPath, SpecHistory history, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RecordRunAsync(string projectPath, IReadOnlyList<SpecRunRecord> results, CancellationToken ct = default)
            => Task.CompletedTask;

        public IReadOnlyList<FlakySpec> GetFlakySpecs(SpecHistory history, int minStatusChanges = 2, int windowSize = 10)
            => [];

        public IReadOnlySet<string> GetQuarantinedSpecIds(SpecHistory history, int minStatusChanges = 2, int windowSize = 10)
            => new HashSet<string>();

        public Task<bool> ClearSpecAsync(string projectPath, string specId, CancellationToken ct = default)
            => Task.FromResult(false);
    }

    private class NullRuntimeEstimator : IRuntimeEstimator
    {
        public RuntimeEstimate Calculate(SpecHistory history, int percentile = 50)
            => new()
            {
                P50Ms = 0,
                P95Ms = 0,
                MaxMs = 0,
                TotalEstimateMs = 0,
                Percentile = percentile,
                SampleSize = 0,
                SpecCount = 0,
                SlowestSpecs = []
            };

        public double CalculatePercentile(IReadOnlyList<double> values, int percentile)
            => 0;
    }

    private class NullSpecSelector : ISpecSelector
    {
        public Task<SpecSelectionResult> SelectAsync(
            IReadOnlyList<DiscoveredSpec> specs,
            CancellationToken ct = default)
            => Task.FromResult(SpecSelectionResult.Success([], [], 0));
    }

    private class NullWatchEventProcessor : IWatchEventProcessor
    {
        public Task<WatchAction> ProcessChangeAsync(
            FileChangeInfo change,
            IReadOnlyList<string> allSpecFiles,
            string basePath,
            bool incremental,
            bool noCache,
            CancellationToken ct)
        {
            // Return RunFile for specific spec file changes, RunAll for everything else
            if (change.IsSpecFile && change.FilePath != null)
                return Task.FromResult(WatchAction.RunFile(change.FilePath));
            return Task.FromResult(WatchAction.RunAll());
        }
    }

    #endregion
}

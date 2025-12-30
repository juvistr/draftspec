using DraftSpec.Cli;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
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
        public bool DirectoryExists(string path) => true;
        public void CreateDirectory(string path) { }
        public string[] GetFiles(string path, string searchPattern) => [];
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [];
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => [];
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => [];
        public DateTime GetLastWriteTimeUtc(string path) => DateTime.MinValue;
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
        public IFormatter? GetFormatter(string name, CliOptions? options = null) => null;
        public void Register(string name, Func<CliOptions?, IFormatter> factory) { }
        public IEnumerable<string> Names => [];
    }

    private class NullConfigLoader : IConfigLoader
    {
        public ConfigLoadResult Load(string? path = null) => new(null, null, null);
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
        public IFileWatcher Create(string path, Action<FileChangeInfo> onChange, int debounceMs = 200) => new NullFileWatcher();
    }

    private class NullFileWatcher : IFileWatcher
    {
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

    #endregion
}

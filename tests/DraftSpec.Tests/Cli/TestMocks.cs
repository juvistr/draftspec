using DraftSpec.Cli;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;
using DraftSpec.Cli.Watch;
using DraftSpec.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Shared mock implementations for CLI tests.
/// </summary>
public static class TestMocks
{
    /// <summary>
    /// A console mock that captures output for assertions.
    /// </summary>
    public class MockConsole : IConsole
    {
        private readonly List<string> _output = [];

        public string Output => string.Join("", _output);

        public void Write(string text) => _output.Add(text);
        public void WriteLine(string text) => _output.Add(text + "\n");
        public void WriteLine() => _output.Add("\n");
        public ConsoleColor ForegroundColor { get; set; }
        public void ResetColor() { }
        public void Clear() { }
        public void WriteWarning(string text) => WriteLine(text);
        public void WriteSuccess(string text) => WriteLine(text);
        public void WriteError(string text) => WriteLine(text);
    }

    /// <summary>
    /// A no-op console that discards all output.
    /// </summary>
    public class NullConsole : IConsole
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

    /// <summary>
    /// A no-op file system mock.
    /// </summary>
    public class NullFileSystem : IFileSystem
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

    /// <summary>
    /// A file system that tracks writes for assertions.
    /// </summary>
    public class TrackingFileSystem : IFileSystem
    {
        public List<(string Path, string Content)> WrittenFiles { get; } = [];

        public bool FileExists(string path) => false;
        public void WriteAllText(string path, string content) => WrittenFiles.Add((path, content));
        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        {
            WrittenFiles.Add((path, content));
            return Task.CompletedTask;
        }
        public string ReadAllText(string path) => "";
        public bool DirectoryExists(string path) => true;
        public void CreateDirectory(string path) { }
        public string[] GetFiles(string path, string searchPattern) => [];
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [];
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => [];
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => [];
        public DateTime GetLastWriteTimeUtc(string path) => DateTime.MinValue;
    }

    /// <summary>
    /// A no-op spec finder.
    /// </summary>
    public class NullSpecFinder : ISpecFinder
    {
        public IReadOnlyList<string> FindSpecs(string path, string? baseDirectory = null) => [];
    }

    /// <summary>
    /// A no-op runner factory.
    /// </summary>
    public class NullRunnerFactory : IInProcessSpecRunnerFactory
    {
        public IInProcessSpecRunner Create(
            string? filterTags = null,
            string? excludeTags = null,
            string? filterName = null,
            string? excludeName = null,
            IReadOnlyList<string>? filterContext = null,
            IReadOnlyList<string>? excludeContext = null) => new NullRunner();
    }

    /// <summary>
    /// A no-op spec runner.
    /// </summary>
    public class NullRunner : IInProcessSpecRunner
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

    /// <summary>
    /// A no-op formatter registry.
    /// </summary>
    public class NullFormatterRegistry : ICliFormatterRegistry
    {
        public IFormatter? GetFormatter(string name, CliOptions? options = null) => null;
        public void Register(string name, Func<CliOptions?, IFormatter> factory) { }
        public IEnumerable<string> Names => [];
    }

    /// <summary>
    /// A config loader that returns empty config.
    /// </summary>
    public class NullConfigLoader : IConfigLoader
    {
        private readonly string? _error;

        public NullConfigLoader(string? error = null) => _error = error;

        public ConfigLoadResult Load(string? path = null)
        {
            if (_error != null)
                return new ConfigLoadResult(null, _error, null);

            return new ConfigLoadResult(null, null, null);
        }
    }

    /// <summary>
    /// A no-op environment.
    /// </summary>
    public class NullEnvironment : IEnvironment
    {
        public string CurrentDirectory => Directory.GetCurrentDirectory();
        public string NewLine => Environment.NewLine;
    }

    /// <summary>
    /// A no-op stats collector.
    /// </summary>
    public class NullStatsCollector : ISpecStatsCollector
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

    /// <summary>
    /// A no-op partitioner that returns all files unmodified.
    /// </summary>
    public class NullPartitioner : ISpecPartitioner
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

    /// <summary>
    /// A no-op file watcher factory.
    /// </summary>
    public class NullFileWatcherFactory : IFileWatcherFactory
    {
        public IFileWatcher Create(string path, Action<FileChangeInfo> onChange, int debounceMs = 200) => new NullFileWatcher();
    }

    /// <summary>
    /// A no-op file watcher.
    /// </summary>
    public class NullFileWatcher : IFileWatcher
    {
        public void Dispose() { }
    }

    /// <summary>
    /// A no-op project resolver.
    /// </summary>
    public class NullProjectResolver : IProjectResolver
    {
        public string? FindProject(string directory) => null;
        public ProjectInfo? GetProjectInfo(string csprojPath) => null;
    }

    /// <summary>
    /// A no-op spec change tracker.
    /// </summary>
    public class NullSpecChangeTracker : ISpecChangeTracker
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
}

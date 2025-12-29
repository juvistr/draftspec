using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for CommandFactory class.
/// </summary>
public class CommandFactoryTests
{
    #region Create Command Tests

    [Test]
    public async Task Create_Run_ReturnsRunCommand()
    {
        var runCommand = new MockRunCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<RunCommand>(runCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("run");

        await Assert.That(command).IsSameReferenceAs(runCommand);
    }

    [Test]
    public async Task Create_Watch_ReturnsWatchCommand()
    {
        var watchCommand = new MockWatchCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<WatchCommand>(watchCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("watch");

        await Assert.That(command).IsSameReferenceAs(watchCommand);
    }

    [Test]
    public async Task Create_List_ReturnsListCommand()
    {
        var listCommand = new MockListCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<ListCommand>(listCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("list");

        await Assert.That(command).IsSameReferenceAs(listCommand);
    }

    [Test]
    public async Task Create_Init_ReturnsInitCommand()
    {
        var initCommand = new MockInitCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<InitCommand>(initCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("init");

        await Assert.That(command).IsSameReferenceAs(initCommand);
    }

    [Test]
    public async Task Create_New_ReturnsNewCommand()
    {
        var newCommand = new MockNewCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<NewCommand>(newCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("new");

        await Assert.That(command).IsSameReferenceAs(newCommand);
    }

    [Test]
    public async Task Create_Unknown_ReturnsNull()
    {
        var serviceProvider = new MockServiceProvider();

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("unknown-command");

        await Assert.That(command).IsNull();
    }

    #endregion

    #region Case Insensitivity Tests

    [Test]
    [Arguments("RUN")]
    [Arguments("Run")]
    [Arguments("run")]
    [Arguments("rUn")]
    public async Task Create_CaseInsensitive_Works(string commandName)
    {
        var runCommand = new MockRunCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<RunCommand>(runCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(runCommand);
    }

    [Test]
    [Arguments("WATCH")]
    [Arguments("Watch")]
    [Arguments("wAtCh")]
    public async Task Create_Watch_CaseInsensitive(string commandName)
    {
        var watchCommand = new MockWatchCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<WatchCommand>(watchCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(watchCommand);
    }

    [Test]
    [Arguments("LIST")]
    [Arguments("List")]
    [Arguments("LiSt")]
    public async Task Create_List_CaseInsensitive(string commandName)
    {
        var listCommand = new MockListCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<ListCommand>(listCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(listCommand);
    }

    [Test]
    [Arguments("INIT")]
    [Arguments("Init")]
    [Arguments("iNiT")]
    public async Task Create_Init_CaseInsensitive(string commandName)
    {
        var initCommand = new MockInitCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<InitCommand>(initCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(initCommand);
    }

    [Test]
    [Arguments("NEW")]
    [Arguments("New")]
    [Arguments("nEw")]
    public async Task Create_New_CaseInsensitive(string commandName)
    {
        var newCommand = new MockNewCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<NewCommand>(newCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(newCommand);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Create_EmptyString_ReturnsNull()
    {
        var serviceProvider = new MockServiceProvider();
        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("");

        await Assert.That(command).IsNull();
    }

    [Test]
    public async Task Create_Whitespace_ReturnsNull()
    {
        var serviceProvider = new MockServiceProvider();
        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("   ");

        await Assert.That(command).IsNull();
    }

    [Test]
    public async Task Create_WithExtraWhitespace_ReturnsNull()
    {
        // The factory uses ToLowerInvariant() which doesn't trim
        // so "run " becomes "run " which doesn't match "run"
        var runCommand = new MockRunCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<RunCommand>(runCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("run ");

        await Assert.That(command).IsNull();
    }

    #endregion

    #region Mock Implementations

    /// <summary>
    /// Simple mock IServiceProvider that returns pre-configured service instances.
    /// </summary>
    private class MockServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];

        public MockServiceProvider Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
            return this;
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }

    private class MockRunCommand : RunCommand
    {
        public MockRunCommand() : base(
            new NullSpecFinder(),
            new NullRunnerFactory(),
            new NullConsole(),
            new NullFormatterRegistry(),
            new NullConfigLoader(),
            new NullFileSystem(),
            new NullEnvironment())
        {
        }
    }

    private class MockWatchCommand : WatchCommand
    {
        public MockWatchCommand() : base(
            new NullSpecFinder(),
            new NullRunnerFactory(),
            new NullFileWatcherFactory(),
            new NullConsole(),
            new NullConfigLoader())
        {
        }
    }

    private class MockListCommand : ListCommand
    {
        public MockListCommand() : base(
            new NullConsole(),
            new NullFileSystem())
        {
        }
    }

    private class MockInitCommand : InitCommand
    {
        public MockInitCommand() : base(
            new NullConsole(),
            new NullFileSystem(),
            new NullProjectResolver())
        {
        }
    }

    private class MockNewCommand : NewCommand
    {
        public MockNewCommand() : base(
            new NullConsole(),
            new NullFileSystem())
        {
        }
    }

    #endregion

    #region Null Implementations for Constructor Injection

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

    private class NullFormatterRegistry : ICliFormatterRegistry
    {
        public IFormatter? GetFormatter(string name, CliOptions? options = null) => null;
        public void Register(string name, Func<CliOptions?, IFormatter> factory) { }
        public IEnumerable<string> Names => [];
    }

    private class NullConfigLoader : IConfigLoader
    {
        public ConfigLoadResult Load(string? path = null)
            => new(null, null, null);
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

    private class NullEnvironment : IEnvironment
    {
        public string CurrentDirectory => Directory.GetCurrentDirectory();
        public string NewLine => System.Environment.NewLine;
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

    #endregion
}

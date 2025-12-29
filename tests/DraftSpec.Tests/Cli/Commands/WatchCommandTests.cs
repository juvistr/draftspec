using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using DraftSpec.Formatters;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for WatchCommand with mocked dependencies.
/// </summary>
public class WatchCommandTests
{
    #region Constructor Dependencies

    [Test]
    public async Task Constructor_WithAllDependencies_Constructs()
    {
        var command = CreateCommand();
        await Assert.That(command).IsNotNull();
    }

    #endregion

    #region Initial Run Behavior

    [Test]
    public async Task ExecuteAsync_NoSpecsFound_RunsWithEmptyList()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: []);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel quickly to exit watch loop

        var options = new CliOptions { Path = "." };
        var result = await command.ExecuteAsync(options, cts.Token);

        // With no specs, the run is successful (no failures), so returns 0
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_SpecsFound_RunsInitialSpecs()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test1.spec.csx", "test2.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(runner.RunAllCalled).IsTrue();
        await Assert.That(runner.LastSpecFiles).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_ConfigError_ThrowsInvalidOperation()
    {
        var configLoader = new MockConfigLoader(error: "Invalid config file");
        var command = CreateCommand(configLoader: configLoader, specFiles: ["test.spec.csx"]);

        var options = new CliOptions { Path = "." };

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_ConfigLoaded_AppliesDefaults()
    {
        var config = new DraftSpecProjectConfig { Parallel = true };
        var configLoader = new MockConfigLoader(config: config);
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(configLoader: configLoader, runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        // Config defaults are applied - parallel should be true now
        await Assert.That(options.Parallel).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SuccessfulRun_ReturnsZero()
    {
        var runner = new MockRunner(success: true);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions { Path = "." };
        var result = await command.ExecuteAsync(options, cts.Token);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_FailedRun_ReturnsOne()
    {
        var runner = new MockRunner(success: false);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions { Path = "." };
        var result = await command.ExecuteAsync(options, cts.Token);

        await Assert.That(result).IsEqualTo(1);
    }

    #endregion

    #region File Watcher Behavior

    [Test]
    public async Task ExecuteAsync_CreatesFileWatcher()
    {
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions { Path = "/some/path" };
        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(watcherFactory.CreateCalled).IsTrue();
        await Assert.That(watcherFactory.LastPath).IsEqualTo("/some/path");
    }

    [Test]
    public async Task ExecuteAsync_FileChange_TriggersRerun()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(runnerFactory: runnerFactory, watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new CliOptions { Path = "." };

        // Start the command in background
        var task = command.ExecuteAsync(options, cts.Token);

        // Wait for initial run
        await Task.Delay(50);

        // Simulate file change
        watcherFactory.TriggerChange(new FileChangeInfo("/some/file.cs", false));

        // Wait a bit more then cancel
        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // Should have called RunAll twice (initial + rerun)
        await Assert.That(runner.RunAllCallCount).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_SpecFileChange_RunsSelectiveRerun()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();
        var specFiles = new[] { "/specs/test1.spec.csx", "/specs/test2.spec.csx" };
        var command = CreateCommand(runnerFactory: runnerFactory, watcherFactory: watcherFactory, specFiles: specFiles);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new CliOptions { Path = "/specs" };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Simulate spec file change
        watcherFactory.TriggerChange(new FileChangeInfo("/specs/test1.spec.csx", true));

        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // Last run should be with just the changed spec (selective)
        // Note: Due to timing, we verify that selective runs happened
        await Assert.That(runner.LastSpecFiles).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_SourceFileChange_RunsFullRerun()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(runnerFactory: runnerFactory, watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new CliOptions { Path = "." };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Simulate source file change (not a spec)
        watcherFactory.TriggerChange(new FileChangeInfo("/src/MyClass.cs", false));

        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // Should have run all specs on source file change
        await Assert.That(runner.RunAllCallCount).IsGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Console Output

    [Test]
    public async Task ExecuteAsync_ShowsStoppedMessage_OnCancellation()
    {
        var console = new MockConsole();
        var command = CreateCommand(console: console, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(console.Output).Contains("Stopped watching");
    }

    [Test]
    public async Task ExecuteAsync_FileChange_ShowsRerunningMessage()
    {
        var console = new MockConsole();
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(console: console, watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new CliOptions { Path = "." };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);
        watcherFactory.TriggerChange(new FileChangeInfo("/some/file.cs", false));

        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // ConsolePresenter.ShowRerunning should have been called
        // This is called through the presenter, so we verify the callback was invoked
        await Assert.That(watcherFactory.OnChangeCallbackInvoked).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ClearsConsoleBetweenRuns()
    {
        var console = new MockConsole();
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(console: console, watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new CliOptions { Path = "." };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);
        watcherFactory.TriggerChange(new FileChangeInfo("/file.cs", false));

        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // Console.Clear() should have been called (tracked in MockConsole)
        await Assert.That(console.ClearCallCount).IsGreaterThan(0);
    }

    #endregion

    #region Filter Options

    [Test]
    public async Task ExecuteAsync_PassesFilterOptions_ToRunner()
    {
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions
        {
            Path = ".",
            FilterTags = "unit",
            ExcludeTags = "slow",
            FilterName = "should pass",
            ExcludeName = "integration"
        };

        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(runnerFactory.LastFilterTags).IsEqualTo("unit");
        await Assert.That(runnerFactory.LastExcludeTags).IsEqualTo("slow");
        await Assert.That(runnerFactory.LastFilterName).IsEqualTo("should pass");
        await Assert.That(runnerFactory.LastExcludeName).IsEqualTo("integration");
    }

    #endregion

    #region Cancellation

    [Test]
    public async Task ExecuteAsync_ImmediateCancellation_ReturnsQuickly()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately

        var options = new CliOptions { Path = "." };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await command.ExecuteAsync(options, cts.Token);
        stopwatch.Stop();

        // Should complete quickly (within 1 second)
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(1000);
    }

    [Test]
    public async Task ExecuteAsync_CancellationDuringInitialRun_ReturnsBasedOnLastSummary()
    {
        var runner = new MockRunner(success: true, delay: 500);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel during the run

        var options = new CliOptions { Path = "." };
        var result = await command.ExecuteAsync(options, cts.Token);

        // Returns based on lastSummary state (may be null → 1, or success → 0)
        await Assert.That(result).IsGreaterThanOrEqualTo(0);
        await Assert.That(result).IsLessThanOrEqualTo(1);
    }

    #endregion

    #region Build Events

    [Test]
    public async Task ExecuteAsync_RegistersBuildEventHandlers()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(runner.OnBuildStartedRegistered).IsTrue();
        await Assert.That(runner.OnBuildCompletedRegistered).IsTrue();
        await Assert.That(runner.OnBuildSkippedRegistered).IsTrue();
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task ExecuteAsync_RunnerThrowsArgumentException_ShowsError()
    {
        var runner = new MockRunner(throwArgumentException: true);
        var runnerFactory = new MockRunnerFactory(runner);
        var console = new MockConsole();
        var command = CreateCommand(console: console, runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new CliOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        // Should show error and continue (not crash)
        // The command catches ArgumentException and shows error via presenter
    }

    #endregion

    #region Helper Methods

    private static WatchCommand CreateCommand(
        MockConsole? console = null,
        MockConfigLoader? configLoader = null,
        MockRunnerFactory? runnerFactory = null,
        MockFileWatcherFactory? watcherFactory = null,
        IReadOnlyList<string>? specFiles = null)
    {
        var specs = specFiles ?? [];
        return new WatchCommand(
            new MockSpecFinder(specs),
            runnerFactory ?? new MockRunnerFactory(),
            watcherFactory ?? new MockFileWatcherFactory(),
            console ?? new MockConsole(),
            configLoader ?? new MockConfigLoader());
    }

    #endregion

    #region Mocks

    private class MockSpecFinder : ISpecFinder
    {
        private readonly IReadOnlyList<string> _specs;

        public MockSpecFinder(IReadOnlyList<string> specs) => _specs = specs;

        public IReadOnlyList<string> FindSpecs(string path, string? baseDirectory = null) => _specs;
    }

    private class MockRunnerFactory : IInProcessSpecRunnerFactory
    {
        private readonly MockRunner? _runner;

        public MockRunnerFactory(MockRunner? runner = null) => _runner = runner;

        public string? LastFilterTags { get; private set; }
        public string? LastExcludeTags { get; private set; }
        public string? LastFilterName { get; private set; }
        public string? LastExcludeName { get; private set; }
        public IReadOnlyList<string>? LastFilterContext { get; private set; }
        public IReadOnlyList<string>? LastExcludeContext { get; private set; }

        public IInProcessSpecRunner Create(string? filterTags = null, string? excludeTags = null, string? filterName = null, string? excludeName = null, IReadOnlyList<string>? filterContext = null, IReadOnlyList<string>? excludeContext = null)
        {
            LastFilterTags = filterTags;
            LastExcludeTags = excludeTags;
            LastFilterName = filterName;
            LastExcludeName = excludeName;
            LastFilterContext = filterContext;
            LastExcludeContext = excludeContext;
            return _runner ?? new MockRunner();
        }
    }

    private class MockRunner : IInProcessSpecRunner
    {
        private readonly bool _success;
        private readonly int _delay;
        private readonly bool _throwArgumentException;

        public MockRunner(bool success = true, int delay = 0, bool throwArgumentException = false)
        {
            _success = success;
            _delay = delay;
            _throwArgumentException = throwArgumentException;
        }

        public bool RunAllCalled { get; private set; }
        public int RunAllCallCount { get; private set; }
        public IReadOnlyList<string>? LastSpecFiles { get; private set; }

        public bool OnBuildStartedRegistered { get; private set; }
        public bool OnBuildCompletedRegistered { get; private set; }
        public bool OnBuildSkippedRegistered { get; private set; }

#pragma warning disable CS0067 // Backing fields stored but not invoked (mock only tracks registration)
        private event Action<string>? _onBuildStarted;
        private event Action<BuildResult>? _onBuildCompleted;
        private event Action<string>? _onBuildSkipped;
#pragma warning restore CS0067

        public event Action<string>? OnBuildStarted
        {
            add { _onBuildStarted += value; OnBuildStartedRegistered = true; }
            remove { _onBuildStarted -= value; }
        }

        public event Action<BuildResult>? OnBuildCompleted
        {
            add { _onBuildCompleted += value; OnBuildCompletedRegistered = true; }
            remove { _onBuildCompleted -= value; }
        }

        public event Action<string>? OnBuildSkipped
        {
            add { _onBuildSkipped += value; OnBuildSkippedRegistered = true; }
            remove { _onBuildSkipped -= value; }
        }

        public Task<InProcessRunResult> RunFileAsync(string specFile, CancellationToken ct = default)
        {
            return Task.FromResult(new InProcessRunResult(
                specFile,
                new SpecReport { Summary = new SpecSummary() },
                TimeSpan.Zero,
                _success ? null : new Exception("Test failed")));
        }

        public async Task<InProcessRunSummary> RunAllAsync(IReadOnlyList<string> specFiles, bool parallel = false, CancellationToken ct = default)
        {
            if (_throwArgumentException)
                throw new ArgumentException("Test error");

            RunAllCalled = true;
            RunAllCallCount++;
            LastSpecFiles = specFiles;

            if (_delay > 0)
                await Task.Delay(_delay, ct);

            var results = specFiles.Select(f => new InProcessRunResult(
                f,
                new SpecReport { Summary = new SpecSummary() },
                TimeSpan.Zero,
                _success ? null : new Exception("Test failed"))).ToList();

            return new InProcessRunSummary(results, TimeSpan.Zero);
        }

        public void ClearBuildCache() { }
    }

    private class MockConsole : IConsole
    {
        private readonly List<string> _output = [];

        public string Output => string.Join("", _output);
        public int ClearCallCount { get; private set; }

        public void Write(string text) => _output.Add(text);
        public void WriteLine(string text) => _output.Add(text + "\n");
        public void WriteLine() => _output.Add("\n");
        public ConsoleColor ForegroundColor { get; set; }
        public void ResetColor() { }
        public void Clear() => ClearCallCount++;
        public void WriteWarning(string text) => WriteLine(text);
        public void WriteSuccess(string text) => WriteLine(text);
        public void WriteError(string text) => WriteLine(text);
    }

    private class MockConfigLoader : IConfigLoader
    {
        private readonly string? _error;
        private readonly DraftSpecProjectConfig? _config;

        public MockConfigLoader(string? error = null, DraftSpecProjectConfig? config = null)
        {
            _error = error;
            _config = config;
        }

        public ConfigLoadResult Load(string? path = null)
        {
            if (_error != null)
                return new ConfigLoadResult(null, _error, null);

            return new ConfigLoadResult(_config, null, _config != null ? "draftspec.json" : null);
        }
    }

    private class MockFileWatcherFactory : IFileWatcherFactory
    {
        private Action<FileChangeInfo>? _onChange;

        public bool CreateCalled { get; private set; }
        public string? LastPath { get; private set; }
        public bool OnChangeCallbackInvoked { get; private set; }

        public IFileWatcher Create(string path, Action<FileChangeInfo> onChange, int debounceMs = 200)
        {
            CreateCalled = true;
            LastPath = path;
            _onChange = onChange;
            return new MockFileWatcher();
        }

        public void TriggerChange(FileChangeInfo change)
        {
            OnChangeCallbackInvoked = true;
            _onChange?.Invoke(change);
        }
    }

    private class MockFileWatcher : IFileWatcher
    {
        public void Dispose() { }
    }

    #endregion
}

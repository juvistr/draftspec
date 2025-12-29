using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for RunCommand with mocked dependencies.
/// </summary>
public class RunCommandTests
{
    #region Constructor Dependencies

    [Test]
    public async Task Constructor_WithAllDependencies_Constructs()
    {
        var command = CreateCommand();
        await Assert.That(command).IsNotNull();
    }

    #endregion

    #region ExecuteAsync Behavior

    [Test]
    public async Task ExecuteAsync_NoSpecsFound_ReturnsZero()
    {
        var console = new MockConsole();
        var command = CreateCommand(console: console, specFiles: []);

        var options = new CliOptions { Path = "." };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No spec files found");
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
    public async Task ExecuteAsync_WithSpecs_RunsAll()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test1.spec.csx", "test2.spec.csx"]);

        var options = new CliOptions { Path = "." };
        var result = await command.ExecuteAsync(options);

        await Assert.That(runner.RunAllCalled).IsTrue();
        await Assert.That(runner.LastSpecFiles).HasCount().EqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_SuccessfulRun_ReturnsZero()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);

        var options = new CliOptions { Path = "." };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_FailedRun_ReturnsOne()
    {
        var runner = new MockRunner(success: false);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var options = new CliOptions { Path = "." };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    #endregion

    #region Helper Methods

    private static RunCommand CreateCommand(
        MockConsole? console = null,
        MockConfigLoader? configLoader = null,
        MockRunnerFactory? runnerFactory = null,
        IReadOnlyList<string>? specFiles = null)
    {
        var specs = specFiles ?? [];
        return new RunCommand(
            new MockSpecFinder(specs),
            runnerFactory ?? new MockRunnerFactory(),
            console ?? new MockConsole(),
            new MockFormatterRegistry(),
            configLoader ?? new MockConfigLoader(),
            new MockFileSystem());
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

        public IInProcessSpecRunner Create(string? filterTags = null, string? excludeTags = null, string? filterName = null, string? excludeName = null)
        {
            return _runner ?? new MockRunner();
        }
    }

    private class MockRunner : IInProcessSpecRunner
    {
        private readonly bool _success;

        public MockRunner(bool success = true) => _success = success;

        public bool RunAllCalled { get; private set; }
        public IReadOnlyList<string>? LastSpecFiles { get; private set; }

        public event Action<string>? OnBuildStarted;
        public event Action<BuildResult>? OnBuildCompleted;
        public event Action<string>? OnBuildSkipped;

        public Task<InProcessRunResult> RunFileAsync(string specFile, CancellationToken ct = default)
        {
            return Task.FromResult(new InProcessRunResult(
                specFile,
                new SpecReport { Summary = new SpecSummary() },
                TimeSpan.Zero,
                _success ? null : new Exception("Test failed")));
        }

        public Task<InProcessRunSummary> RunAllAsync(IReadOnlyList<string> specFiles, bool parallel = false, CancellationToken ct = default)
        {
            RunAllCalled = true;
            LastSpecFiles = specFiles;

            var results = specFiles.Select(f => new InProcessRunResult(
                f,
                new SpecReport { Summary = new SpecSummary() },
                TimeSpan.Zero,
                _success ? null : new Exception("Test failed"))).ToList();

            return Task.FromResult(new InProcessRunSummary(results, TimeSpan.Zero));
        }

        public void ClearBuildCache() { }
    }

    private class MockConsole : IConsole
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

    private class MockFormatterRegistry : ICliFormatterRegistry
    {
        public IFormatter? GetFormatter(string name, CliOptions? options = null) => null;
        public void Register(string name, Func<CliOptions?, IFormatter> factory) { }
        public IEnumerable<string> Names => [];
    }

    private class MockConfigLoader : IConfigLoader
    {
        private readonly string? _error;

        public MockConfigLoader(string? error = null) => _error = error;

        public ConfigLoadResult Load(string? path = null)
        {
            if (_error != null)
                return new ConfigLoadResult(null, _error, null);

            return new ConfigLoadResult(null, null, null);
        }
    }

    private class MockFileSystem : IFileSystem
    {
        public bool FileExists(string path) => false;
        public void WriteAllText(string path, string content) { }
        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default) => Task.CompletedTask;
        public string ReadAllText(string path) => "";
        public bool DirectoryExists(string path) => true;
        public void CreateDirectory(string path) { }
    }

    #endregion
}

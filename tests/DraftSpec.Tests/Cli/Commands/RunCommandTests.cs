using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

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
        var configLoader = new ErrorConfigLoader("Invalid config file");
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
        await Assert.That(runner.LastSpecFiles).Count().IsEqualTo(2);
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

    #region Format and Output Tests

    [Test]
    public async Task ExecuteAsync_JsonFormat_ReturnsJsonOutput()
    {
        var console = new MockConsole();
        // failedCount must be 0 for Success to be true
        var runner = new MockRunner(success: true, passedCount: 5, failedCount: 0);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["test.spec.csx"]);

        var options = new CliOptions { Path = ".", Format = OutputFormat.Json };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("\"summary\"");
        await Assert.That(console.Output).Contains("\"passed\":");
    }

    // Note: Unknown format validation now happens at parse time in CliOptionsParser,
    // not at execution time. Test removed since we can't create an invalid enum value.

    [Test]
    public async Task ExecuteAsync_ConsoleFormat_DisplaysDirectly()
    {
        var console = new MockConsole();
        var runner = new MockRunner(success: true, passedCount: 3);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["test.spec.csx"]);

        var options = new CliOptions { Path = ".", Format = OutputFormat.Console };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // Console format shows summary info with file count and status
        await Assert.That(console.Output).Contains("1 spec file");
        await Assert.That(console.Output).Contains("PASS");
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_WritesToFileAndShowsMessage()
    {
        var console = new MockConsole();
        var fileSystem = new MockFileSystem();
        var command = CreateCommand(
            console: console,
            specFiles: ["test.spec.csx"],
            fileSystem: fileSystem);

        var options = new CliOptions { Path = ".", Format = OutputFormat.Json, OutputFile = "report.json" };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(fileSystem.WrittenFiles.Count).IsEqualTo(1);
        await Assert.That(console.Output).Contains("Report written to");
        await Assert.That(console.Output).Contains("report.json");
    }

    [Test]
    public async Task ExecuteAsync_NoOutputFile_OutputsToConsole()
    {
        var console = new MockConsole();
        var runner = new MockRunner(success: true, passedCount: 1);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["test.spec.csx"]);

        var options = new CliOptions { Path = ".", Format = OutputFormat.Json };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("\"summary\"");
    }

    #endregion

    #region MergeReports Tests (via ExecuteAsync)

    [Test]
    public async Task ExecuteAsync_SingleResult_PreservesCounts()
    {
        var console = new MockConsole();
        var runner = new MockRunner(success: true, passedCount: 5, failedCount: 0, pendingCount: 2);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["single.spec.csx"]);

        var options = new CliOptions { Path = ".", Format = OutputFormat.Json };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("\"passed\": 5");
        await Assert.That(console.Output).Contains("\"pending\": 2");
    }

    [Test]
    public async Task ExecuteAsync_MultipleResults_AggregatesCounts()
    {
        var console = new MockConsole();
        // failedCount > 0 means the run fails (returns 1)
        var runner = new MockRunner(success: true, passedCount: 3, failedCount: 1, pendingCount: 1);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["first.spec.csx", "second.spec.csx", "third.spec.csx"]);

        var options = new CliOptions { Path = ".", Format = OutputFormat.Json };
        var result = await command.ExecuteAsync(options);

        // With failed tests, return code is 1
        await Assert.That(result).IsEqualTo(1);
        // 3 files × 3 passed each = 9 passed
        await Assert.That(console.Output).Contains("\"passed\": 9");
        // 3 files × 1 failed each = 3 failed
        await Assert.That(console.Output).Contains("\"failed\": 3");
    }

    [Test]
    public async Task ExecuteAsync_EmptyResults_ReturnsEmptyReport()
    {
        var console = new MockConsole();
        // Infrastructure's MockRunner defaults to Total=1 when all counts are 0
        var runner = new MockRunner(success: true, passedCount: 0, failedCount: 0);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["empty.spec.csx"]);

        var options = new CliOptions { Path = ".", Format = OutputFormat.Json };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // MockRunner defaults to total=1 when all counts are 0
        await Assert.That(console.Output).Contains("\"total\": 1");
    }

    [Test]
    public async Task ExecuteAsync_MixedPassFail_SumsCorrectly()
    {
        var console = new MockConsole();
        var runner = new MockRunner(success: false, passedCount: 10, failedCount: 2, pendingCount: 0, skippedCount: 3);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["mixed.spec.csx"]);

        var options = new CliOptions { Path = ".", Format = OutputFormat.Json };
        var result = await command.ExecuteAsync(options);

        // Failed run returns 1
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("\"passed\": 10");
        await Assert.That(console.Output).Contains("\"failed\": 2");
        await Assert.That(console.Output).Contains("\"skipped\": 3");
    }

    #endregion

    #region Parallel Execution Tests

    [Test]
    public async Task ExecuteAsync_ParallelTrue_PassesParallelFlag()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var options = new CliOptions { Path = ".", Parallel = true };
        await command.ExecuteAsync(options);

        await Assert.That(runner.LastParallelFlag).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ParallelFalse_PassesSequentialFlag()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var options = new CliOptions { Path = ".", Parallel = false };
        await command.ExecuteAsync(options);

        await Assert.That(runner.LastParallelFlag).IsFalse();
    }

    #endregion

    #region OutputFile Security Tests

    [Test]
    public async Task ExecuteAsync_OutputFile_PathTraversal_ThrowsSecurityException()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);
        var options = new CliOptions { Path = ".", OutputFile = "../../../etc/malicious.json" };

        await Assert.ThrowsAsync<System.Security.SecurityException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_DoubleDots_ThrowsSecurityException()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);
        var options = new CliOptions { Path = ".", OutputFile = "../output.json" };

        await Assert.ThrowsAsync<System.Security.SecurityException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_ParentDirectory_ThrowsSecurityException()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);
        var options = new CliOptions { Path = ".", OutputFile = ".." };

        await Assert.ThrowsAsync<System.Security.SecurityException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    [Arguments("/etc/passwd")]
    [Arguments("/tmp/outside/output.json")]
    public async Task ExecuteAsync_OutputFile_AbsolutePathOutsideCurrentDir_ThrowsSecurityException(string outputPath)
    {
        // Skip on Windows since these paths don't make sense there
        if (OperatingSystem.IsWindows())
            return;

        var command = CreateCommand(specFiles: ["test.spec.csx"]);
        var options = new CliOptions { Path = ".", OutputFile = outputPath };

        await Assert.ThrowsAsync<System.Security.SecurityException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_ValidRelativePath_DoesNotThrow()
    {
        var fileSystem = new MockFileSystem();
        var command = CreateCommand(specFiles: ["test.spec.csx"], fileSystem: fileSystem);
        var options = new CliOptions { Path = ".", OutputFile = "output.json" };

        // Should not throw SecurityException - may fail for other reasons but that's okay
        var result = await command.ExecuteAsync(options);

        // If we get here, no SecurityException was thrown
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_SubdirectoryPath_DoesNotThrow()
    {
        var fileSystem = new MockFileSystem();
        var command = CreateCommand(specFiles: ["test.spec.csx"], fileSystem: fileSystem);
        var options = new CliOptions { Path = ".", OutputFile = "reports/output.json" };

        // Should not throw SecurityException
        var result = await command.ExecuteAsync(options);

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_CurrentDirPrefix_DoesNotThrow()
    {
        var fileSystem = new MockFileSystem();
        var command = CreateCommand(specFiles: ["test.spec.csx"], fileSystem: fileSystem);
        var options = new CliOptions { Path = ".", OutputFile = "./output.json" };

        // Should not throw SecurityException
        var result = await command.ExecuteAsync(options);

        await Assert.That(true).IsTrue();
    }

    #endregion

    #region Helper Methods

    private static RunCommand CreateCommand(
        MockConsole? console = null,
        IConfigLoader? configLoader = null,
        IInProcessSpecRunnerFactory? runnerFactory = null,
        IReadOnlyList<string>? specFiles = null,
        IFileSystem? fileSystem = null,
        IEnvironment? environment = null)
    {
        var specs = specFiles ?? [];
        return new RunCommand(
            new MockSpecFinder(specs),
            runnerFactory ?? NullObjects.RunnerFactory,
            console ?? new MockConsole(),
            NullObjects.FormatterRegistry,
            configLoader ?? NullObjects.ConfigLoader,
            fileSystem ?? NullObjects.FileSystem,
            environment ?? NullObjects.Environment,
            NullObjects.StatsCollector,
            NullObjects.Partitioner);
    }

    #endregion

    #region Local Config Loader

    /// <summary>
    /// Config loader that supports error injection for testing.
    /// </summary>
    private class ErrorConfigLoader : IConfigLoader
    {
        private readonly string _error;

        public ErrorConfigLoader(string error) => _error = error;

        public ConfigLoadResult Load(string? path = null) => new(null, _error, null);
    }

    #endregion
}

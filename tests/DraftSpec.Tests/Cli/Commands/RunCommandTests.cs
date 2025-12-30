using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.Services;
using DraftSpec.Formatters;
using DraftSpec.Tests.TestHelpers;

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

        var options = new CliOptions { Path = ".", Format = OutputFormats.Json };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("\"summary\"");
        await Assert.That(console.Output).Contains("\"passed\":");
    }

    [Test]
    public async Task ExecuteAsync_UnknownFormat_ThrowsArgumentException()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);
        var options = new CliOptions { Path = ".", Format = "unknown-format" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

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

        var options = new CliOptions { Path = ".", Format = OutputFormats.Console };
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
        var fileSystem = new TrackingFileSystem();
        var command = CreateCommand(
            console: console,
            specFiles: ["test.spec.csx"],
            fileSystem: fileSystem);

        var options = new CliOptions { Path = ".", Format = OutputFormats.Json, OutputFile = "report.json" };
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

        var options = new CliOptions { Path = ".", Format = OutputFormats.Json };
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

        var options = new CliOptions { Path = ".", Format = OutputFormats.Json };
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

        var options = new CliOptions { Path = ".", Format = OutputFormats.Json };
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
        var runner = new MockRunner(success: true, passedCount: 0, failedCount: 0);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["empty.spec.csx"]);

        var options = new CliOptions { Path = ".", Format = OutputFormats.Json };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("\"total\": 0");
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

        var options = new CliOptions { Path = ".", Format = OutputFormats.Json };
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
        var fileSystem = new TrackingFileSystem();
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
        var fileSystem = new TrackingFileSystem();
        var command = CreateCommand(specFiles: ["test.spec.csx"], fileSystem: fileSystem);
        var options = new CliOptions { Path = ".", OutputFile = "reports/output.json" };

        // Should not throw SecurityException
        var result = await command.ExecuteAsync(options);

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_CurrentDirPrefix_DoesNotThrow()
    {
        var fileSystem = new TrackingFileSystem();
        var command = CreateCommand(specFiles: ["test.spec.csx"], fileSystem: fileSystem);
        var options = new CliOptions { Path = ".", OutputFile = "./output.json" };

        // Should not throw SecurityException
        var result = await command.ExecuteAsync(options);

        await Assert.That(true).IsTrue();
    }

    #endregion

    #region Line Filter Tests

    private string _tempDir = null!;

    [Before(Test)]
    public void SetupTempDir()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_run_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void CleanupTempDir()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_FindsExactLineMatch()
    {
        // Create a spec file
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
                it("subtracts numbers", () => { });
            });
            """);

        var console = new MockConsole();
        var runnerFactory = new TrackingRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Line 4 should match "adds numbers"
        var options = new CliOptions
        {
            Path = _tempDir,
            LineFilters = [new LineFilter("test.spec.csx", [4])]
        };

        await command.ExecuteAsync(options);

        // The filter should contain the spec description (escaped for regex)
        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        // Regex.Escape converts spaces to "\ "
        await Assert.That(runnerFactory.LastFilterName!).Contains(@"adds\ numbers");
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_FindsNearbySpec()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
            });
            """);

        var console = new MockConsole();
        var runnerFactory = new TrackingRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Line 5 is within 1 line of "adds numbers" (line 4)
        var options = new CliOptions
        {
            Path = _tempDir,
            LineFilters = [new LineFilter("test.spec.csx", [5])]
        };

        await command.ExecuteAsync(options);

        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        await Assert.That(runnerFactory.LastFilterName!).Contains(@"adds\ numbers");
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_FileNotFound_ShowsWarning()
    {
        var console = new MockConsole();
        var command = CreateCommand(
            console: console,
            specFiles: [],
            fileSystem: new RealFileSystem());

        var options = new CliOptions
        {
            Path = _tempDir,
            LineFilters = [new LineFilter("nonexistent.spec.csx", [1])]
        };

        await command.ExecuteAsync(options);

        await Assert.That(console.Output).Contains("File not found");
        await Assert.That(console.Output).Contains("nonexistent.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_MultipleLines_CombinesPattern()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
                it("subtracts numbers", () => { });
            });
            """);

        var console = new MockConsole();
        var runnerFactory = new TrackingRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Lines 4 and 5 should match both specs
        var options = new CliOptions
        {
            Path = _tempDir,
            LineFilters = [new LineFilter("test.spec.csx", [4, 5])]
        };

        await command.ExecuteAsync(options);

        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        await Assert.That(runnerFactory.LastFilterName!).Contains(@"adds\ numbers");
        await Assert.That(runnerFactory.LastFilterName!).Contains(@"subtracts\ numbers");
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_MergesWithExistingFilterName()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
            });
            """);

        var console = new MockConsole();
        var runnerFactory = new TrackingRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        var options = new CliOptions
        {
            Path = _tempDir,
            FilterName = "existing",
            LineFilters = [new LineFilter("test.spec.csx", [4])]
        };

        await command.ExecuteAsync(options);

        // Should combine both filters with OR
        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        await Assert.That(runnerFactory.LastFilterName!).Contains("existing");
        await Assert.That(runnerFactory.LastFilterName!).Contains(@"adds\ numbers");
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_IncludesContextPath()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                describe("addition", () =>
                {
                    it("handles positive numbers", () => { });
                });
            });
            """);

        var console = new MockConsole();
        var runnerFactory = new TrackingRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        var options = new CliOptions
        {
            Path = _tempDir,
            LineFilters = [new LineFilter("test.spec.csx", [6])]
        };

        await command.ExecuteAsync(options);

        // Should include full display name with context path (escaped for regex)
        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        await Assert.That(runnerFactory.LastFilterName!).Contains("Calculator");
        await Assert.That(runnerFactory.LastFilterName!).Contains("addition");
        await Assert.That(runnerFactory.LastFilterName!).Contains(@"handles\ positive\ numbers");
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_NoSpecsAtLine_ShowsError()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
            });
            """);

        var console = new MockConsole();
        var command = CreateCommand(
            console: console,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Line 100 is way past the end of the file - no specs there
        var options = new CliOptions
        {
            Path = _tempDir,
            LineFilters = [new LineFilter("test.spec.csx", [100])]
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("No specs found at the specified line numbers");
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_DuplicateSpecs_Deduplicated()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
            });
            """);

        var console = new MockConsole();
        var runnerFactory = new TrackingRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Lines 4 and 5 both match the same spec "adds numbers"
        // The filter should deduplicate
        var options = new CliOptions
        {
            Path = _tempDir,
            LineFilters = [new LineFilter("test.spec.csx", [4, 5])]
        };

        await command.ExecuteAsync(options);

        // The filter name should only have the spec once (not duplicated)
        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        // Count occurrences of "adds numbers" - should be 1
        var filterName = runnerFactory.LastFilterName!;
        var count = System.Text.RegularExpressions.Regex.Matches(filterName, @"adds\\ numbers").Count;
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_LineFilter_SpecWithNoContext_UsesDescriptionOnly()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        // Top-level spec with no describe block
        await File.WriteAllTextAsync(specPath, """
            using static DraftSpec.Dsl;
            it("standalone test", () => { });
            """);

        var console = new MockConsole();
        var runnerFactory = new TrackingRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        var options = new CliOptions
        {
            Path = _tempDir,
            LineFilters = [new LineFilter("test.spec.csx", [2])]
        };

        await command.ExecuteAsync(options);

        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        await Assert.That(runnerFactory.LastFilterName!).Contains(@"standalone\ test");
        // Should not contain context separator when there's no context
        await Assert.That(runnerFactory.LastFilterName!).DoesNotContain(">");
    }

    #endregion

    #region Helper Methods

    private static RunCommand CreateCommand(
        MockConsole? console = null,
        MockConfigLoader? configLoader = null,
        IInProcessSpecRunnerFactory? runnerFactory = null,
        IReadOnlyList<string>? specFiles = null,
        IFileSystem? fileSystem = null,
        IEnvironment? environment = null)
    {
        var specs = specFiles ?? [];
        return new RunCommand(
            new MockSpecFinder(specs),
            runnerFactory ?? new MockRunnerFactory(),
            console ?? new MockConsole(),
            new MockFormatterRegistry(),
            configLoader ?? new MockConfigLoader(),
            fileSystem ?? new MockFileSystem(),
            environment ?? new MockEnvironment(),
            new MockStatsCollector(),
            new MockPartitioner());
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

        public IInProcessSpecRunner Create(string? filterTags = null, string? excludeTags = null, string? filterName = null, string? excludeName = null, IReadOnlyList<string>? filterContext = null, IReadOnlyList<string>? excludeContext = null)
        {
            return _runner ?? new MockRunner();
        }
    }

    private class TrackingRunnerFactory : IInProcessSpecRunnerFactory
    {
        public string? LastFilterName { get; private set; }
        public string? LastFilterTags { get; private set; }
        public IReadOnlyList<string>? LastFilterContext { get; private set; }

        public IInProcessSpecRunner Create(string? filterTags = null, string? excludeTags = null, string? filterName = null, string? excludeName = null, IReadOnlyList<string>? filterContext = null, IReadOnlyList<string>? excludeContext = null)
        {
            LastFilterTags = filterTags;
            LastFilterName = filterName;
            LastFilterContext = filterContext;
            return new MockRunner();
        }
    }

    private class MockRunner : IInProcessSpecRunner
    {
        private readonly bool _success;
        private readonly int _passedCount;
        private readonly int _failedCount;
        private readonly int _pendingCount;
        private readonly int _skippedCount;

        public MockRunner(
            bool success = true,
            int passedCount = 0,
            int failedCount = 0,
            int pendingCount = 0,
            int skippedCount = 0)
        {
            _success = success;
            _passedCount = passedCount;
            _failedCount = failedCount;
            _pendingCount = pendingCount;
            _skippedCount = skippedCount;
        }

        public bool RunAllCalled { get; private set; }
        public IReadOnlyList<string>? LastSpecFiles { get; private set; }
        public bool LastParallelFlag { get; private set; }

#pragma warning disable CS0067 // Events required by interface but not used in mock
        public event Action<string>? OnBuildStarted;
        public event Action<BuildResult>? OnBuildCompleted;
        public event Action<string>? OnBuildSkipped;
#pragma warning restore CS0067

        public Task<InProcessRunResult> RunFileAsync(string specFile, CancellationToken ct = default)
        {
            return Task.FromResult(CreateResult(specFile));
        }

        public Task<InProcessRunSummary> RunAllAsync(IReadOnlyList<string> specFiles, bool parallel = false, CancellationToken ct = default)
        {
            RunAllCalled = true;
            LastSpecFiles = specFiles;
            LastParallelFlag = parallel;

            var results = specFiles.Select(CreateResult).ToList();

            return Task.FromResult(new InProcessRunSummary(results, TimeSpan.Zero));
        }

        private InProcessRunResult CreateResult(string specFile)
        {
            var total = _passedCount + _failedCount + _pendingCount + _skippedCount;
            return new InProcessRunResult(
                specFile,
                new SpecReport
                {
                    Summary = new SpecSummary
                    {
                        Total = total,
                        Passed = _passedCount,
                        Failed = _failedCount,
                        Pending = _pendingCount,
                        Skipped = _skippedCount
                    }
                },
                TimeSpan.Zero,
                _success ? null : new Exception("Test failed"));
        }

        public void ClearBuildCache() { }
    }

    // Aliases for shared mocks from TestMocks
    private class MockConsole : TestMocks.MockConsole { }
    private class MockFormatterRegistry : TestMocks.NullFormatterRegistry { }
    private class MockConfigLoader : TestMocks.NullConfigLoader
    {
        public MockConfigLoader(string? error = null) : base(error) { }
    }
    private class MockFileSystem : TestMocks.NullFileSystem { }
    private class TrackingFileSystem : TestMocks.TrackingFileSystem { }
    private class MockStatsCollector : TestMocks.NullStatsCollector { }
    private class MockPartitioner : TestMocks.NullPartitioner { }
    private class MockEnvironment : TestMocks.NullEnvironment { }

    private class RealFileSystem : IFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);
        public string ReadAllText(string path) => File.ReadAllText(path);
        public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
            => File.WriteAllTextAsync(path, content, ct);
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public string[] GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
            => Directory.GetFiles(path, searchPattern, searchOption);
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
            => Directory.EnumerateFiles(path, searchPattern, searchOption);
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
            => Directory.EnumerateDirectories(path, searchPattern);
        public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
    }

    #endregion
}

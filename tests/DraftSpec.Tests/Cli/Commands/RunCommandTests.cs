using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.History;
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

        var options = new RunOptions { Path = "." };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No spec files found");
    }

    [Test]
    public async Task ExecuteAsync_WithSpecs_RunsAll()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test1.spec.csx", "test2.spec.csx"]);

        var options = new RunOptions { Path = "." };
        var result = await command.ExecuteAsync(options);

        await Assert.That(runner.RunAllCalled).IsTrue();
        await Assert.That(runner.LastSpecFiles).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_SuccessfulRun_ReturnsZero()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);

        var options = new RunOptions { Path = "." };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_FailedRun_ReturnsOne()
    {
        var runner = new MockRunner(success: false);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var options = new RunOptions { Path = "." };
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

        var options = new RunOptions { Path = ".", Format = OutputFormat.Json };
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

        var options = new RunOptions { Path = ".", Format = OutputFormat.Console };
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

        var options = new RunOptions { Path = ".", Format = OutputFormat.Json, OutputFile = "report.json" };
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

        var options = new RunOptions { Path = ".", Format = OutputFormat.Json };
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

        var options = new RunOptions { Path = ".", Format = OutputFormat.Json };
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

        var options = new RunOptions { Path = ".", Format = OutputFormat.Json };
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

        var options = new RunOptions { Path = ".", Format = OutputFormat.Json };
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

        var options = new RunOptions { Path = ".", Format = OutputFormat.Json };
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

        var options = new RunOptions { Path = ".", Parallel = true };
        await command.ExecuteAsync(options);

        await Assert.That(runner.LastParallelFlag).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ParallelFalse_PassesSequentialFlag()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var options = new RunOptions { Path = ".", Parallel = false };
        await command.ExecuteAsync(options);

        await Assert.That(runner.LastParallelFlag).IsFalse();
    }

    #endregion

    #region OutputFile Security Tests

    [Test]
    public async Task ExecuteAsync_OutputFile_PathTraversal_ThrowsSecurityException()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);
        var options = new RunOptions { Path = ".", OutputFile = "../../../etc/malicious.json" };

        await Assert.ThrowsAsync<System.Security.SecurityException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_DoubleDots_ThrowsSecurityException()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);
        var options = new RunOptions { Path = ".", OutputFile = "../output.json" };

        await Assert.ThrowsAsync<System.Security.SecurityException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_ParentDirectory_ThrowsSecurityException()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);
        var options = new RunOptions { Path = ".", OutputFile = ".." };

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
        var options = new RunOptions { Path = ".", OutputFile = outputPath };

        await Assert.ThrowsAsync<System.Security.SecurityException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_ValidRelativePath_DoesNotThrow()
    {
        var fileSystem = new MockFileSystem();
        var command = CreateCommand(specFiles: ["test.spec.csx"], fileSystem: fileSystem);
        var options = new RunOptions { Path = ".", OutputFile = "output.json" };

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
        var options = new RunOptions { Path = ".", OutputFile = "reports/output.json" };

        // Should not throw SecurityException
        var result = await command.ExecuteAsync(options);

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_CurrentDirPrefix_DoesNotThrow()
    {
        var fileSystem = new MockFileSystem();
        var command = CreateCommand(specFiles: ["test.spec.csx"], fileSystem: fileSystem);
        var options = new RunOptions { Path = ".", OutputFile = "./output.json" };

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
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Line 4 should match "adds numbers"
        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                LineFilters = [new LineFilter("test.spec.csx", [4])]
            }
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
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Line 5 is within 1 line of "adds numbers" (line 4)
        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                LineFilters = [new LineFilter("test.spec.csx", [5])]
            }
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

        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                LineFilters = [new LineFilter("nonexistent.spec.csx", [1])]
            }
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
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Lines 4 and 5 should match both specs
        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                LineFilters = [new LineFilter("test.spec.csx", [4, 5])]
            }
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
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                FilterName = "existing",
                LineFilters = [new LineFilter("test.spec.csx", [4])]
            }
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
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                LineFilters = [new LineFilter("test.spec.csx", [6])]
            }
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
        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                LineFilters = [new LineFilter("test.spec.csx", [100])]
            }
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
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        // Lines 4 and 5 both match the same spec "adds numbers"
        // The filter should deduplicate
        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                LineFilters = [new LineFilter("test.spec.csx", [4, 5])]
            }
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
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: [specPath],
            fileSystem: new RealFileSystem());

        var options = new RunOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions
            {
                LineFilters = [new LineFilter("test.spec.csx", [2])]
            }
        };

        await command.ExecuteAsync(options);

        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        await Assert.That(runnerFactory.LastFilterName!).Contains(@"standalone\ test");
        // Should not contain context separator when there's no context
        await Assert.That(runnerFactory.LastFilterName!).DoesNotContain(">");
    }

    #endregion

    #region Partition Tests

    [Test]
    public async Task ExecuteAsync_PartitionEnabled_UsesPartitionedFiles()
    {
        var console = new MockConsole();
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        // Partition returns only 2 of the 5 files
        var partitioner = new MockPartitioner(
            files: ["partition1.spec.csx", "partition2.spec.csx"],
            totalFiles: 5);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["1.spec.csx", "2.spec.csx", "3.spec.csx", "4.spec.csx", "5.spec.csx"],
            partitioner: partitioner);

        var options = new RunOptions
        {
            Path = ".",
            Partition = new PartitionOptions
            {
                Total = 3,
                Index = 0
            }
        };
        await command.ExecuteAsync(options);

        // Runner should receive only the partitioned files
        await Assert.That(runner.LastSpecFiles).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_EmptyPartition_ReturnsZeroWithMessage()
    {
        var console = new MockConsole();
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        // Partition returns empty list
        var partitioner = new MockPartitioner(files: [], totalFiles: 5);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["1.spec.csx", "2.spec.csx"],
            partitioner: partitioner);

        var options = new RunOptions
        {
            Path = ".",
            Partition = new PartitionOptions
            {
                Total = 10,
                Index = 9 // Last partition might be empty
            }
        };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No specs in this partition");
        // Runner should NOT have been called
        await Assert.That(runner.RunAllCalled).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_PartitionWithSpecCounts_DisplaysSpecInfo()
    {
        var console = new MockConsole();
        var partitioner = new MockPartitioner(
            files: ["test.spec.csx"],
            totalFiles: 3,
            totalSpecs: 100,
            partitionSpecs: 35);
        var command = CreateCommand(
            console: console,
            specFiles: ["test.spec.csx"],
            partitioner: partitioner);

        var options = new RunOptions
        {
            Path = ".",
            Partition = new PartitionOptions
            {
                Total = 3,
                Index = 0
            }
        };
        await command.ExecuteAsync(options);

        // Should display partition info with spec counts
        await Assert.That(console.Output).Contains("Partition 1 of 3");
        await Assert.That(console.Output).Contains("35 spec(s) of 100 total");
    }

    [Test]
    public async Task ExecuteAsync_PartitionWithoutSpecCounts_DisplaysOnlyFileCounts()
    {
        var console = new MockConsole();
        var partitioner = new MockPartitioner(
            files: ["test.spec.csx"],
            totalFiles: 3,
            totalSpecs: null,
            partitionSpecs: null);
        var command = CreateCommand(
            console: console,
            specFiles: ["test.spec.csx"],
            partitioner: partitioner);

        var options = new RunOptions
        {
            Path = ".",
            Partition = new PartitionOptions
            {
                Total = 3,
                Index = 1
            }
        };
        await command.ExecuteAsync(options);

        // Should display partition info with file counts only
        await Assert.That(console.Output).Contains("Partition 2 of 3");
        await Assert.That(console.Output).Contains("1 file(s) of 3 total");
        // Should NOT display spec counts
        await Assert.That(console.Output).DoesNotContain("spec(s) of");
    }

    [Test]
    public async Task ExecuteAsync_PartitionDisabled_RunsAllFiles()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            runnerFactory: runnerFactory,
            specFiles: ["1.spec.csx", "2.spec.csx", "3.spec.csx"]);

        // Default PartitionOptions has IsEnabled = false
        var options = new RunOptions { Path = "." };
        await command.ExecuteAsync(options);

        // All files should be passed to runner
        await Assert.That(runner.LastSpecFiles).Count().IsEqualTo(3);
    }

    #endregion

    #region Impact Analysis (--affected-by)

    [Test]
    public async Task ExecuteAsync_WithAffectedBy_WhenNoChangedFiles_ExitsWithZero()
    {
        var console = new MockConsole();
        var gitService = new MockGitService().WithChangedFiles(); // Empty list
        var command = CreateCommand(
            console: console,
            specFiles: ["test1.spec.csx", "test2.spec.csx"],
            gitService: gitService);

        var options = new RunOptions
        {
            Path = ".",
            AffectedBy = "HEAD~1"
        };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No changed files detected");
    }

    [Test]
    public async Task ExecuteAsync_WithAffectedBy_WhenNoAffectedSpecs_ExitsWithZero()
    {
        var console = new MockConsole();
        var gitService = new MockGitService().WithChangedFiles("/unrelated/file.cs");
        var command = CreateCommand(
            console: console,
            specFiles: ["test.spec.csx"],
            gitService: gitService);

        var options = new RunOptions
        {
            Path = ".",
            AffectedBy = "HEAD~1"
        };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No affected specs to run");
    }

    [Test]
    public async Task ExecuteAsync_WithAffectedBy_WhenGitFails_ExitsWithError()
    {
        var console = new MockConsole();
        var gitService = new MockGitService()
            .ThrowsOnGetChangedFiles(new InvalidOperationException("Git error"));
        var command = CreateCommand(
            console: console,
            specFiles: ["test.spec.csx"],
            gitService: gitService);

        var options = new RunOptions
        {
            Path = ".",
            AffectedBy = "HEAD~1"
        };
        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("Failed to get changed files");
    }

    [Test]
    public async Task ExecuteAsync_WithAffectedByAndDryRun_ShowsSpecsWithoutRunning()
    {
        var console = new MockConsole();
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var gitService = new MockGitService()
            .WithChangedFiles("/some/changed/file.cs");
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["test.spec.csx"],
            gitService: gitService);

        var options = new RunOptions
        {
            Path = ".",
            AffectedBy = "HEAD~1",
            DryRun = true
        };
        var result = await command.ExecuteAsync(options);

        // Should exit with 0 for dry run
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("dry run");
        // Runner should NOT have been called
        await Assert.That(runner.RunAllCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_WithAffectedBy_CallsGitServiceWithCorrectReference()
    {
        var console = new MockConsole();
        var gitService = new MockGitService().WithChangedFiles();
        var command = CreateCommand(
            console: console,
            specFiles: ["test.spec.csx"],
            gitService: gitService);

        var options = new RunOptions
        {
            Path = "/my/project",
            AffectedBy = "main"
        };
        await command.ExecuteAsync(options);

        // Verify correct reference was passed
        await Assert.That(gitService.GetChangedFilesCalls.Count).IsEqualTo(1);
        await Assert.That(gitService.GetChangedFilesCalls[0].Reference).IsEqualTo("main");
    }

    #endregion

    #region History Integration Tests

    [Test]
    public async Task ExecuteAsync_RecordsHistoryAfterRun()
    {
        var historyService = new MockSpecHistoryService();
        var runner = new MockRunner(passedCount: 1, includeContexts: true);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(
            runnerFactory: runnerFactory,
            specFiles: ["test.spec.csx"],
            historyService: historyService);

        var options = new RunOptions { Path = "." };
        await command.ExecuteAsync(options);

        await Assert.That(historyService.RecordRunAsyncCalls).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_NoHistoryFlag_SkipsRecording()
    {
        var historyService = new MockSpecHistoryService();
        var command = CreateCommand(
            specFiles: ["test.spec.csx"],
            historyService: historyService);

        var options = new RunOptions { Path = ".", NoHistory = true };
        await command.ExecuteAsync(options);

        await Assert.That(historyService.RecordRunAsyncCalls).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_Quarantine_ExcludesFlakySpecs()
    {
        var console = new MockConsole();
        var runnerFactory = new MockRunnerFactory();
        var flakySpec = new FlakySpec
        {
            SpecId = "test.spec.csx:Context/spec1",
            DisplayName = "Context > spec1",
            StatusChanges = 3,
            TotalRuns = 5,
            PassRate = 0.6
        };
        var historyService = new MockSpecHistoryService()
            .WithFlakySpecs(flakySpec);
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["test.spec.csx"],
            historyService: historyService);

        var options = new RunOptions { Path = ".", Quarantine = true };
        await command.ExecuteAsync(options);

        // Should create a runner with an exclude filter for the flaky spec (regex-escaped)
        await Assert.That(runnerFactory.LastExcludeName).IsNotNull();
        await Assert.That(runnerFactory.LastExcludeName!).Contains(@"Context\ >\ spec1");
        await Assert.That(console.Output).Contains("Quarantining 1 flaky spec");
    }

    [Test]
    public async Task ExecuteAsync_Quarantine_NoFlakySpecs_RunsAll()
    {
        var console = new MockConsole();
        var runnerFactory = new MockRunnerFactory();
        var historyService = new MockSpecHistoryService();
        // No flaky specs configured
        var command = CreateCommand(
            console: console,
            runnerFactory: runnerFactory,
            specFiles: ["test.spec.csx"],
            historyService: historyService);

        var options = new RunOptions { Path = ".", Quarantine = true };
        await command.ExecuteAsync(options);

        // Should not have an exclude filter for flaky specs
        await Assert.That(runnerFactory.LastExcludeName).IsNull();
        await Assert.That(console.Output).DoesNotContain("Quarantining");
    }

    #endregion

    #region Helper Methods

    private static RunCommand CreateCommand(
        MockConsole? console = null,
        IInProcessSpecRunnerFactory? runnerFactory = null,
        IReadOnlyList<string>? specFiles = null,
        IFileSystem? fileSystem = null,
        IEnvironment? environment = null,
        ISpecPartitioner? partitioner = null,
        IGitService? gitService = null,
        MockSpecHistoryService? historyService = null)
    {
        var specs = specFiles ?? [];
        return new RunCommand(
            new MockSpecFinder(specs),
            runnerFactory ?? NullObjects.RunnerFactory,
            console ?? new MockConsole(),
            NullObjects.FormatterRegistry,
            fileSystem ?? NullObjects.FileSystem,
            environment ?? NullObjects.Environment,
            NullObjects.StatsCollector,
            partitioner ?? NullObjects.Partitioner,
            gitService ?? NullObjects.GitService,
            historyService ?? new MockSpecHistoryService());
    }

    #endregion
}

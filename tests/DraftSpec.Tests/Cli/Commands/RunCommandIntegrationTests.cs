using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Services;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Integration tests for RunCommand that use real file I/O operations.
/// These tests verify line filter functionality with actual spec files.
/// </summary>
public class RunCommandIntegrationTests
{
    #region Test Setup

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

    #endregion

    #region Line Filter Tests

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

    #region Helper Methods

    private static RunCommand CreateCommand(
        MockConsole? console = null,
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
            fileSystem ?? NullObjects.FileSystem,
            environment ?? NullObjects.Environment,
            NullObjects.StatsCollector,
            NullObjects.Partitioner,
            NullObjects.GitService);
    }

    #endregion
}

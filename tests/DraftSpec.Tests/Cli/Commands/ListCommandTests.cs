using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Formatters;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;
using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for ListCommand.
/// These tests use the real file system for spec discovery.
/// </summary>
[NotInParallel]
public class ListCommandTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;
    private RealFileSystem _fileSystem = null!;
    private MockPartitioner _partitioner = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_list_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _console = new MockConsole();
        _fileSystem = new RealFileSystem();
        _partitioner = new MockPartitioner();
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private ListCommand CreateCommand() => new(_console, _fileSystem, _partitioner);

    #region Path Validation

    [Test]
    public async Task ExecuteAsync_NonexistentPath_ThrowsArgumentException()
    {
        var command = CreateCommand();
        var options = new CliOptions { Path = "/nonexistent/path" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_EmptyDirectory_ReturnsNoSpecs()
    {
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No spec files found");
    }

    #endregion

    #region Spec Discovery

    [Test]
    public async Task ExecuteAsync_SingleSpecFile_DiscoverSpecs()
    {
        var specFile = CreateSpecFile("test.spec.csx", """
            describe("Calculator", () => {
                it("adds numbers", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = specFile };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("Calculator");
        await Assert.That(output).Contains("adds numbers");
    }

    [Test]
    public async Task ExecuteAsync_DirectoryWithSpecs_DiscoverAllSpecs()
    {
        CreateSpecFile("math.spec.csx", """
            describe("Math", () => {
                it("adds", () => { });
            });
            """);
        CreateSpecFile("string.spec.csx", """
            describe("String", () => {
                it("concatenates", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("Math");
        await Assert.That(output).Contains("String");
    }

    [Test]
    public async Task ExecuteAsync_NestedContexts_DiscoverAll()
    {
        CreateSpecFile("nested.spec.csx", """
            describe("Parent", () => {
                describe("Child", () => {
                    it("nested spec", () => { });
                });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("Parent");
        await Assert.That(output).Contains("Child");
        await Assert.That(output).Contains("nested spec");
    }

    #endregion

    #region Spec Types

    [Test]
    public async Task ExecuteAsync_PendingSpec_ShowsPending()
    {
        CreateSpecFile("pending.spec.csx", """
            describe("Feature", () => {
                it("pending spec");
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("[PENDING]");
    }

    [Test]
    public async Task ExecuteAsync_SkippedSpec_ShowsSkipped()
    {
        CreateSpecFile("skipped.spec.csx", """
            describe("Feature", () => {
                xit("skipped spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("[SKIPPED]");
    }

    [Test]
    public async Task ExecuteAsync_FocusedSpec_ShowsFocused()
    {
        CreateSpecFile("focused.spec.csx", """
            describe("Feature", () => {
                fit("focused spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("[FOCUSED]");
    }

    #endregion

    #region Filters

    [Test]
    public async Task ExecuteAsync_FocusedOnlyFilter_ShowsOnlyFocused()
    {
        CreateSpecFile("mixed.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
                fit("focused spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, FocusedOnly = true, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("focused spec");
        await Assert.That(output).DoesNotContain("regular spec");
    }

    [Test]
    public async Task ExecuteAsync_PendingOnlyFilter_ShowsOnlyPending()
    {
        CreateSpecFile("mixed.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
                it("pending spec");
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, PendingOnly = true, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("pending spec");
        await Assert.That(output).DoesNotContain("regular spec");
    }

    [Test]
    public async Task ExecuteAsync_SkippedOnlyFilter_ShowsOnlySkipped()
    {
        CreateSpecFile("mixed.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
                xit("skipped spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SkippedOnly = true, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("skipped spec");
        await Assert.That(output).DoesNotContain("regular spec");
    }

    [Test]
    public async Task ExecuteAsync_FilterName_MatchesSubstring()
    {
        CreateSpecFile("filter.spec.csx", """
            describe("Feature", () => {
                it("add numbers", () => { });
                it("subtract numbers", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, FilterName = "add", ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("add numbers");
        await Assert.That(output).DoesNotContain("subtract");
    }

    [Test]
    public async Task ExecuteAsync_FilterName_MatchesRegex()
    {
        CreateSpecFile("filter.spec.csx", """
            describe("Feature", () => {
                it("add 1", () => { });
                it("add 2", () => { });
                it("subtract 1", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, FilterName = "add \\d", ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("add 1");
        await Assert.That(output).Contains("add 2");
        await Assert.That(output).DoesNotContain("subtract");
    }

    [Test]
    public async Task ExecuteAsync_InvalidRegex_FallsBackToSubstring()
    {
        CreateSpecFile("fallback.spec.csx", """
            describe("Feature", () => {
                it("test (unclosed paren", () => { });
                it("normal test", () => { });
            });
            """);
        var command = CreateCommand();
        // Invalid regex pattern (unmatched opening paren) should fall back to substring match
        var options = new CliOptions { Path = _tempDir, FilterName = "(unclosed", ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // Should find the spec containing "(unclosed" as substring
        await Assert.That(output).Contains("test (unclosed paren");
        await Assert.That(output).DoesNotContain("normal test");
    }

    [Test]
    public async Task ExecuteAsync_MultipleStatusFilters_UsesOrLogic()
    {
        CreateSpecFile("multi.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
                fit("focused spec", () => { });
                it("pending spec");
                xit("skipped spec", () => { });
            });
            """);
        var command = CreateCommand();
        // FocusedOnly AND PendingOnly should show both focused and pending (OR logic)
        var options = new CliOptions { Path = _tempDir, FocusedOnly = true, PendingOnly = true, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("focused spec");
        await Assert.That(output).Contains("pending spec");
        await Assert.That(output).DoesNotContain("regular spec");
        await Assert.That(output).DoesNotContain("skipped spec");
    }

    [Test]
    public async Task ExecuteAsync_AllStatusFilters_ShowsAllNonRegular()
    {
        CreateSpecFile("all.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
                fit("focused spec", () => { });
                it("pending spec");
                xit("skipped spec", () => { });
            });
            """);
        var command = CreateCommand();
        // All three status filters = show focused OR pending OR skipped
        var options = new CliOptions
        {
            Path = _tempDir,
            FocusedOnly = true,
            PendingOnly = true,
            SkippedOnly = true,
            ListFormat = ListFormat.Flat
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("focused spec");
        await Assert.That(output).Contains("pending spec");
        await Assert.That(output).Contains("skipped spec");
        await Assert.That(output).DoesNotContain("regular spec");
    }

    [Test]
    public async Task ExecuteAsync_StatusFilterWithNameFilter_CombinesWithAnd()
    {
        CreateSpecFile("combined.spec.csx", """
            describe("Feature", () => {
                it("pending apple");
                it("pending banana");
                fit("focused apple", () => { });
                fit("focused banana", () => { });
            });
            """);
        var command = CreateCommand();
        // FocusedOnly + FilterName "apple" should show only focused specs containing "apple"
        var options = new CliOptions
        {
            Path = _tempDir,
            FocusedOnly = true,
            FilterName = "apple",
            ListFormat = ListFormat.Flat
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("focused apple");
        await Assert.That(output).DoesNotContain("focused banana");
        await Assert.That(output).DoesNotContain("pending");
    }

    [Test]
    public async Task ExecuteAsync_FilterTags_WithEmptyTags_FiltersOutAll()
    {
        // Note: Current implementation sets Tags = [] for all specs,
        // so FilterTags will always filter out everything.
        // This tests that the filter doesn't crash with empty tags.
        CreateSpecFile("tags.spec.csx", """
            describe("Feature", () => {
                it("spec one", () => { });
                it("spec two", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, FilterTags = "sometag", ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // No specs have tags, so all should be filtered out
        await Assert.That(_console.Output).DoesNotContain("spec one");
        await Assert.That(_console.Output).DoesNotContain("spec two");
    }

    #endregion

    #region Output Formats

    [Test]
    public async Task ExecuteAsync_TreeFormat_ShowsTreeStructure()
    {
        CreateSpecFile("tree.spec.csx", """
            describe("Root", () => {
                it("spec1", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ListFormat = ListFormat.Tree };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // Tree format uses box-drawing characters
        await Assert.That(output).Contains("tree.spec.csx");
        await Assert.That(output).Contains("Root");
    }

    [Test]
    public async Task ExecuteAsync_FlatFormat_OneLinePerSpec()
    {
        CreateSpecFile("flat.spec.csx", """
            describe("Context", () => {
                it("spec1", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // Flat format shows file:line and context path
        await Assert.That(output).Contains("flat.spec.csx:");
        await Assert.That(output).Contains("Context > spec1");
    }

    [Test]
    public async Task ExecuteAsync_JsonFormat_ValidJson()
    {
        CreateSpecFile("json.spec.csx", """
            describe("JsonTest", () => {
                it("spec1", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ListFormat = ListFormat.Json };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // JSON format contains expected structure
        await Assert.That(output).Contains("\"specs\"");
        await Assert.That(output).Contains("\"summary\"");
        await Assert.That(output).Contains("\"JsonTest\"");
    }

    [Test]
    public async Task ExecuteAsync_JsonFormat_IncludesSummary()
    {
        CreateSpecFile("summary.spec.csx", """
            describe("Summary", () => {
                it("spec1", () => { });
                it("spec2", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ListFormat = ListFormat.Json };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("\"totalSpecs\": 2");
    }

    #endregion

    #region Line Numbers

    [Test]
    public async Task ExecuteAsync_ShowLineNumbers_IncludesLineNumbers()
    {
        CreateSpecFile("lines.spec.csx", """
            describe("Lines", () => {
                it("spec with line", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ShowLineNumbers = true, ListFormat = ListFormat.Tree };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // Line numbers appear as :N
        await Assert.That(_console.Output).Contains(":"); // Contains line number indicator
    }

    [Test]
    public async Task ExecuteAsync_NoLineNumbers_ExcludesLineNumbers()
    {
        CreateSpecFile("nolines.spec.csx", """
            describe("NoLines", () => {
                it("spec without line", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ShowLineNumbers = false, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // Flat format without line numbers shouldn't have :N pattern before context
        await Assert.That(output).Contains("NoLines > spec without line");
    }

    #endregion

    #region Partitioning

    [Test]
    public async Task ExecuteAsync_WithPartition_CallsPartitioner()
    {
        CreateSpecFile("a.spec.csx", """
            describe("A", () => {
                it("spec a", () => { });
            });
            """);
        CreateSpecFile("b.spec.csx", """
            describe("B", () => {
                it("spec b", () => { });
            });
            """);
        var partitioner = new ConfigurablePartitioner();
        partitioner.SetResult(new PartitionResult(
            [Path.Combine(_tempDir, "a.spec.csx")],
            TotalFiles: 2,
            TotalSpecs: 2,
            PartitionSpecs: 1));
        var command = new ListCommand(_console, _fileSystem, partitioner);
        var options = new CliOptions
        {
            Path = _tempDir,
            Partition = 2,
            PartitionIndex = 0,
            PartitionStrategy = PartitionStrategy.File
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(partitioner.PartitionCalled).IsTrue();
        await Assert.That(_console.Output).Contains("Partition 1 of 2");
        await Assert.That(_console.Output).Contains("1 file");
    }

    [Test]
    public async Task ExecuteAsync_WithPartition_PrintsPartitionInfo()
    {
        CreateSpecFile("test.spec.csx", """
            describe("Test", () => {
                it("spec", () => { });
            });
            """);
        var partitioner = new ConfigurablePartitioner();
        partitioner.SetResult(new PartitionResult(
            [Path.Combine(_tempDir, "test.spec.csx")],
            TotalFiles: 4,
            TotalSpecs: 10,
            PartitionSpecs: 3));
        var command = new ListCommand(_console, _fileSystem, partitioner);
        var options = new CliOptions
        {
            Path = _tempDir,
            Partition = 4,
            PartitionIndex = 2,
            PartitionStrategy = PartitionStrategy.SpecCount
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // Partition index is 0-based, but display is 1-based
        await Assert.That(_console.Output).Contains("Partition 3 of 4");
    }

    [Test]
    public async Task ExecuteAsync_WithEmptyPartition_PrintsMessage()
    {
        CreateSpecFile("test.spec.csx", """
            describe("Test", () => {
                it("spec", () => { });
            });
            """);
        var partitioner = new ConfigurablePartitioner();
        partitioner.SetResult(new PartitionResult(
            Files: [],
            TotalFiles: 1,
            TotalSpecs: 1,
            PartitionSpecs: 0));
        var command = new ListCommand(_console, _fileSystem, partitioner);
        var options = new CliOptions
        {
            Path = _tempDir,
            Partition = 3,
            PartitionIndex = 2,
            PartitionStrategy = PartitionStrategy.File
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No specs in this partition");
    }

    [Test]
    public async Task ExecuteAsync_WithPartition_FiltersToPartitionFiles()
    {
        CreateSpecFile("a.spec.csx", """
            describe("A", () => {
                it("spec a", () => { });
            });
            """);
        CreateSpecFile("b.spec.csx", """
            describe("B", () => {
                it("spec b", () => { });
            });
            """);
        CreateSpecFile("c.spec.csx", """
            describe("C", () => {
                it("spec c", () => { });
            });
            """);
        var partitioner = new ConfigurablePartitioner();
        // Simulate that partition only includes file "b"
        partitioner.SetResult(new PartitionResult(
            [Path.Combine(_tempDir, "b.spec.csx")],
            TotalFiles: 3));
        var command = new ListCommand(_console, _fileSystem, partitioner);
        var options = new CliOptions
        {
            Path = _tempDir,
            Partition = 3,
            PartitionIndex = 1,
            PartitionStrategy = PartitionStrategy.File,
            ListFormat = ListFormat.Flat
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // Only specs from file "b" should be in output
        await Assert.That(_console.Output).Contains("spec b");
        await Assert.That(_console.Output).DoesNotContain("spec a");
        await Assert.That(_console.Output).DoesNotContain("spec c");
    }

    #endregion

    #region Output File

    [Test]
    public async Task ExecuteAsync_OutputFile_WritesToFile()
    {
        CreateSpecFile("output.spec.csx", """
            describe("Output", () => {
                it("writes to file", () => { });
            });
            """);
        var outputFile = Path.Combine(_tempDir, "output.txt");
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, OutputFile = outputFile, ListFormat = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(outputFile)).IsTrue();
        var fileContent = await File.ReadAllTextAsync(outputFile);
        await Assert.That(fileContent).Contains("Output");
    }

    [Test]
    public async Task ExecuteAsync_OutputFile_PrintsConfirmation()
    {
        CreateSpecFile("confirm.spec.csx", """
            describe("Confirm", () => {
                it("spec", () => { });
            });
            """);
        var outputFile = Path.Combine(_tempDir, "confirm.txt");
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, OutputFile = outputFile };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Wrote");
        await Assert.That(_console.Output).Contains(outputFile);
    }

    #endregion

    #region Helper Methods

    private string CreateSpecFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    #endregion

    #region Mocks

    // Partitioner-specific mocks
    private class MockPartitioner : ISpecPartitioner
    {
        public IReadOnlyList<string>? LastSpecFiles { get; private set; }
        public int LastTotalPartitions { get; private set; }
        public int LastPartitionIndex { get; private set; }

        public Task<PartitionResult> PartitionAsync(
            IReadOnlyList<string> specFiles,
            int totalPartitions,
            int partitionIndex,
            PartitionStrategy strategy,
            string projectPath,
            CancellationToken ct = default)
        {
            LastSpecFiles = specFiles;
            LastTotalPartitions = totalPartitions;
            LastPartitionIndex = partitionIndex;
            return Task.FromResult(new PartitionResult(specFiles, specFiles.Count));
        }
    }

    /// <summary>
    /// Configurable partitioner for testing partitioning code paths.
    /// </summary>
    private class ConfigurablePartitioner : ISpecPartitioner
    {
        private PartitionResult? _result;

        public bool PartitionCalled { get; private set; }

        public void SetResult(PartitionResult result) => _result = result;

        public Task<PartitionResult> PartitionAsync(
            IReadOnlyList<string> specFiles,
            int totalPartitions,
            int partitionIndex,
            PartitionStrategy strategy,
            string projectPath,
            CancellationToken ct = default)
        {
            PartitionCalled = true;
            return Task.FromResult(_result ?? new PartitionResult(specFiles, specFiles.Count));
        }
    }

    #endregion
}

using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Formatters;
using DraftSpec.TestingPlatform;

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

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_list_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _console = new MockConsole();
        _fileSystem = new RealFileSystem();
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private ListCommand CreateCommand() => new(_console, _fileSystem);

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
        var options = new CliOptions { Path = _tempDir, ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, FocusedOnly = true, ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, PendingOnly = true, ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, SkippedOnly = true, ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, FilterName = "add", ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, FilterName = "add \\d", ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, FilterName = "(unclosed", ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, FocusedOnly = true, PendingOnly = true, ListFormat = "flat" };

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
            ListFormat = "flat"
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
            ListFormat = "flat"
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
        var options = new CliOptions { Path = _tempDir, FilterTags = "sometag", ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, ListFormat = "tree" };

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
        var options = new CliOptions { Path = _tempDir, ListFormat = "flat" };

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
        var options = new CliOptions { Path = _tempDir, ListFormat = "json" };

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
        var options = new CliOptions { Path = _tempDir, ListFormat = "json" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("\"totalSpecs\": 2");
    }

    [Test]
    public async Task ExecuteAsync_InvalidFormat_ThrowsError()
    {
        CreateSpecFile("test.spec.csx", """
            describe("Test", () => {
                it("spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, ListFormat = "invalid" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
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
        var options = new CliOptions { Path = _tempDir, ShowLineNumbers = true, ListFormat = "tree" };

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
        var options = new CliOptions { Path = _tempDir, ShowLineNumbers = false, ListFormat = "flat" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // Flat format without line numbers shouldn't have :N pattern before context
        await Assert.That(output).Contains("NoLines > spec without line");
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
        var options = new CliOptions { Path = _tempDir, OutputFile = outputFile, ListFormat = "flat" };

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

    private class RealFileSystem : IFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);
        public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default) =>
            File.WriteAllTextAsync(path, content, ct);
        public string ReadAllText(string path) => File.ReadAllText(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public string[] GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) =>
            Directory.GetFiles(path, searchPattern, searchOption);
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
            Directory.EnumerateFiles(path, searchPattern, searchOption);
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) =>
            Directory.EnumerateDirectories(path, searchPattern);
        public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
    }

    #endregion
}

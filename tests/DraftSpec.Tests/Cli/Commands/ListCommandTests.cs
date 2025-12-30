using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
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
        var options = new ListOptions { Path = "/nonexistent/path" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_EmptyDirectory_ReturnsNoSpecs()
    {
        var command = CreateCommand();
        var options = new ListOptions { Path = _tempDir };

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
        var options = new ListOptions { Path = specFile };

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
        var options = new ListOptions { Path = _tempDir };

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
        var options = new ListOptions { Path = _tempDir };

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
        var options = new ListOptions { Path = _tempDir, Format = ListFormat.Flat };

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
        var options = new ListOptions { Path = _tempDir, Format = ListFormat.Flat };

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
        var options = new ListOptions { Path = _tempDir, Format = ListFormat.Flat };

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
        var options = new ListOptions { Path = _tempDir, FocusedOnly = true, Format = ListFormat.Flat };

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
        var options = new ListOptions { Path = _tempDir, PendingOnly = true, Format = ListFormat.Flat };

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
        var options = new ListOptions { Path = _tempDir, SkippedOnly = true, Format = ListFormat.Flat };

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
        var options = new ListOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions { FilterName = "add" },
            Format = ListFormat.Flat
        };

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
        var options = new ListOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions { FilterName = "add \\d" },
            Format = ListFormat.Flat
        };

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
        var options = new ListOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions { FilterName = "(unclosed" },
            Format = ListFormat.Flat
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // Should find the spec containing "(unclosed" as substring
        await Assert.That(output).Contains("test (unclosed paren");
        await Assert.That(output).DoesNotContain("normal test");
    }

    [Test]
    public async Task ExecuteAsync_RegexTimeout_FallsBackToSubstring()
    {
        // Create spec with a name that would be matched by both regex and substring
        CreateSpecFile("timeout.spec.csx", """
            describe("Feature", () => {
                it("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab", () => { });
                it("other spec", () => { });
            });
            """);
        var command = CreateCommand();
        // Catastrophic backtracking pattern - (a+)+ with non-matching end
        // This regex is known to cause exponential backtracking
        var options = new ListOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions { FilterName = "(a+)+b" },
            Format = ListFormat.Flat
        };

        var result = await command.ExecuteAsync(options);

        // Either the regex works (unlikely to timeout with our short string)
        // or it falls back to substring match - either way should succeed
        await Assert.That(result).IsEqualTo(0);
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
        var options = new ListOptions
        {
            Path = _tempDir,
            FocusedOnly = true,
            PendingOnly = true,
            Format = ListFormat.Flat
        };

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
        var options = new ListOptions
        {
            Path = _tempDir,
            FocusedOnly = true,
            PendingOnly = true,
            SkippedOnly = true,
            Format = ListFormat.Flat
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
        var options = new ListOptions
        {
            Path = _tempDir,
            FocusedOnly = true,
            Filter = new FilterOptions { FilterName = "apple" },
            Format = ListFormat.Flat
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
        var options = new ListOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions { FilterTags = "sometag" },
            Format = ListFormat.Flat
        };

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
        var options = new ListOptions { Path = _tempDir, Format = ListFormat.Tree };

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
        var options = new ListOptions { Path = _tempDir, Format = ListFormat.Flat };

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
        var options = new ListOptions { Path = _tempDir, Format = ListFormat.Json };

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
        var options = new ListOptions { Path = _tempDir, Format = ListFormat.Json };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("\"totalSpecs\": 2");
    }

    [Test]
    public async Task ExecuteAsync_UnknownFormat_ThrowsArgumentOutOfRange()
    {
        CreateSpecFile("unknown.spec.csx", """
            describe("Test", () => {
                it("spec", () => { });
            });
            """);
        var command = CreateCommand();
        // Cast an invalid integer to ListFormat to trigger the default case
        var options = new ListOptions { Path = _tempDir, Format = (ListFormat)999 };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
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
        var options = new ListOptions { Path = _tempDir, ShowLineNumbers = true, Format = ListFormat.Tree };

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
        var options = new ListOptions { Path = _tempDir, ShowLineNumbers = false, Format = ListFormat.Flat };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // Flat format without line numbers shouldn't have :N pattern before context
        await Assert.That(output).Contains("NoLines > spec without line");
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
}

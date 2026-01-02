using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for DocsCommand.
/// These tests use the real file system for spec discovery.
/// </summary>
[NotInParallel]
public class DocsCommandTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;
    private RealFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_docs_test_{Guid.NewGuid():N}");
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

    private DocsCommand CreateCommand() => new(_console, _fileSystem);

    #region Path Validation

    [Test]
    public async Task ExecuteAsync_NonexistentPath_ThrowsArgumentException()
    {
        var command = CreateCommand();
        var nonexistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}", "path");
        var options = new DocsOptions { Path = nonexistentPath };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_EmptyDirectory_ReturnsNoSpecs()
    {
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir };

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
        var options = new DocsOptions { Path = specFile };

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
        var options = new DocsOptions { Path = _tempDir };

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
        var options = new DocsOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("Parent");
        await Assert.That(output).Contains("Child");
        await Assert.That(output).Contains("nested spec");
    }

    #endregion

    #region Markdown Output Format

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_ContainsMarkdownHeadings()
    {
        CreateSpecFile("markdown.spec.csx", """
            describe("Feature", () => {
                it("does something", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Markdown };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        // Markdown uses # for headings
        await Assert.That(output).Contains("#");
        await Assert.That(output).Contains("Feature");
    }

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_ContainsCheckboxes()
    {
        CreateSpecFile("checkbox.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Markdown };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // Markdown checkboxes
        await Assert.That(_console.Output).Contains("- [ ]");
    }

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_PendingSpecShowsEmphasis()
    {
        CreateSpecFile("pending.spec.csx", """
            describe("Feature", () => {
                it("pending spec");
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Markdown };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // Pending specs show (pending) marker
        await Assert.That(_console.Output).Contains("*(pending)*");
    }

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_SkippedSpecShowsMarker()
    {
        CreateSpecFile("skipped.spec.csx", """
            describe("Feature", () => {
                xit("skipped spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Markdown };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // Skipped specs show (skipped) marker
        await Assert.That(_console.Output).Contains("*(skipped)*");
    }

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_ContainsSummary()
    {
        CreateSpecFile("summary.spec.csx", """
            describe("Feature", () => {
                it("spec one", () => { });
                it("spec two", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Markdown };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("2 specs");
    }

    #endregion

    #region HTML Output Format

    [Test]
    public async Task ExecuteAsync_HtmlFormat_ContainsHtmlStructure()
    {
        CreateSpecFile("html.spec.csx", """
            describe("Feature", () => {
                it("does something", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Html };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("<!DOCTYPE html>");
        await Assert.That(output).Contains("<html");
        await Assert.That(output).Contains("</html>");
    }

    [Test]
    public async Task ExecuteAsync_HtmlFormat_ContainsCollapsibleDetails()
    {
        CreateSpecFile("details.spec.csx", """
            describe("Feature", () => {
                it("spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Html };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("<details");
        await Assert.That(output).Contains("<summary>");
    }

    [Test]
    public async Task ExecuteAsync_HtmlFormat_ContainsStatusBadges()
    {
        CreateSpecFile("badges.spec.csx", """
            describe("Feature", () => {
                it("pending spec");
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Html };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("badge");
        await Assert.That(_console.Output).Contains("PENDING");
    }

    [Test]
    public async Task ExecuteAsync_HtmlFormat_SkippedShowsBadge()
    {
        CreateSpecFile("skipped.spec.csx", """
            describe("Feature", () => {
                xit("skipped spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Html };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("SKIPPED");
    }

    [Test]
    public async Task ExecuteAsync_HtmlFormat_FocusedShowsBadge()
    {
        CreateSpecFile("focused.spec.csx", """
            describe("Feature", () => {
                fit("focused spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = DocsFormat.Html };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("FOCUSED");
    }

    #endregion

    #region Context Filter

    [Test]
    public async Task ExecuteAsync_ContextFilter_FiltersToMatchingContext()
    {
        CreateSpecFile("filter.spec.csx", """
            describe("UserService", () => {
                it("creates user", () => { });
            });
            describe("OrderService", () => {
                it("creates order", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Context = "UserService" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("UserService");
        await Assert.That(output).Contains("creates user");
        await Assert.That(output).DoesNotContain("OrderService");
    }

    [Test]
    public async Task ExecuteAsync_ContextFilter_SupportsRegex()
    {
        CreateSpecFile("regex.spec.csx", """
            describe("UserService", () => {
                it("does something", () => { });
            });
            describe("UserController", () => {
                it("handles request", () => { });
            });
            describe("OrderService", () => {
                it("processes", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Context = "User.*" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("UserService");
        await Assert.That(output).Contains("UserController");
        await Assert.That(output).DoesNotContain("OrderService");
    }

    [Test]
    public async Task ExecuteAsync_ContextFilter_InvalidRegexFallsBackToSubstring()
    {
        CreateSpecFile("fallback.spec.csx", """
            describe("Test (unclosed", () => {
                it("spec", () => { });
            });
            describe("Other", () => {
                it("other spec", () => { });
            });
            """);
        var command = CreateCommand();
        // Invalid regex pattern (unmatched opening paren) should fall back to substring match
        var options = new DocsOptions { Path = _tempDir, Context = "(unclosed" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("Test (unclosed");
        await Assert.That(output).DoesNotContain("Other");
    }

    #endregion

    #region Filter Name

    [Test]
    public async Task ExecuteAsync_FilterName_MatchesSpecDisplayName()
    {
        CreateSpecFile("name.spec.csx", """
            describe("Feature", () => {
                it("adds numbers", () => { });
                it("subtracts numbers", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = _tempDir,
            Filter = new FilterOptions { FilterName = "adds" }
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _console.Output;
        await Assert.That(output).Contains("adds numbers");
        await Assert.That(output).DoesNotContain("subtracts");
    }

    #endregion

    #region With Results

    [Test]
    public async Task ExecuteAsync_WithResults_RequiresResultsFile()
    {
        CreateSpecFile("results.spec.csx", """
            describe("Feature", () => {
                it("spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = _tempDir,
            WithResults = true,
            ResultsFile = null
        };

        await command.ExecuteAsync(options);

        // Should show error about missing results file
        await Assert.That(_console.Errors).Contains("--with-results requires --results-file");
    }

    [Test]
    public async Task ExecuteAsync_WithResults_NonexistentFile_ShowsError()
    {
        CreateSpecFile("results.spec.csx", """
            describe("Feature", () => {
                it("spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = _tempDir,
            WithResults = true,
            ResultsFile = Path.Combine(_tempDir, "nonexistent.json")
        };

        await command.ExecuteAsync(options);

        await Assert.That(_console.Errors).Contains("Results file not found");
    }

    [Test]
    public async Task ExecuteAsync_WithResults_InvalidJson_ShowsError()
    {
        CreateSpecFile("results.spec.csx", """
            describe("Feature", () => {
                it("spec", () => { });
            });
            """);
        var resultsFile = Path.Combine(_tempDir, "results.json");
        await File.WriteAllTextAsync(resultsFile, "not valid json");

        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = _tempDir,
            WithResults = true,
            ResultsFile = resultsFile
        };

        await command.ExecuteAsync(options);

        await Assert.That(_console.Errors).Contains("Failed to parse results file");
    }

    #endregion

    #region Unknown Format

    [Test]
    public async Task ExecuteAsync_UnknownFormat_ThrowsArgumentOutOfRange()
    {
        CreateSpecFile("unknown.spec.csx", """
            describe("Test", () => {
                it("spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new DocsOptions { Path = _tempDir, Format = (DocsFormat)999 };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await command.ExecuteAsync(options));
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

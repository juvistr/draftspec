using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for ListCommand.
/// These tests use the file system for spec discovery.
/// </summary>
[NotInParallel]
public class ListCommandTests
{
    private string _tempDir = null!;
    private TextWriter _originalOut = null!;
    private StringWriter _consoleOutput = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_list_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Capture console output
        _originalOut = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    [After(Test)]
    public void TearDown()
    {
        Console.SetOut(_originalOut);
        _consoleOutput.Dispose();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region Path Validation

    [Test]
    public async Task Execute_NonexistentPath_ReturnsError()
    {
        var options = new CliOptions { Path = "/nonexistent/path" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_consoleOutput.ToString()).Contains("Path not found");
    }

    [Test]
    public async Task Execute_EmptyDirectory_ReturnsNoSpecs()
    {
        var options = new CliOptions { Path = _tempDir };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_consoleOutput.ToString()).Contains("No spec files found");
    }

    #endregion

    #region Spec Discovery

    [Test]
    public async Task Execute_SingleSpecFile_DiscoverSpecs()
    {
        var specFile = CreateSpecFile("test.spec.csx", """
            describe("Calculator", () => {
                it("adds numbers", () => { });
            });
            """);
        var options = new CliOptions { Path = specFile };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("Calculator");
        await Assert.That(output).Contains("adds numbers");
    }

    [Test]
    public async Task Execute_DirectoryWithSpecs_DiscoverAllSpecs()
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
        var options = new CliOptions { Path = _tempDir };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("Math");
        await Assert.That(output).Contains("String");
    }

    [Test]
    public async Task Execute_NestedContexts_DiscoverAll()
    {
        CreateSpecFile("nested.spec.csx", """
            describe("Parent", () => {
                describe("Child", () => {
                    it("nested spec", () => { });
                });
            });
            """);
        var options = new CliOptions { Path = _tempDir };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("Parent");
        await Assert.That(output).Contains("Child");
        await Assert.That(output).Contains("nested spec");
    }

    #endregion

    #region Spec Types

    [Test]
    public async Task Execute_PendingSpec_ShowsPending()
    {
        CreateSpecFile("pending.spec.csx", """
            describe("Feature", () => {
                it("pending spec");
            });
            """);
        var options = new CliOptions { Path = _tempDir, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_consoleOutput.ToString()).Contains("[PENDING]");
    }

    [Test]
    public async Task Execute_SkippedSpec_ShowsSkipped()
    {
        CreateSpecFile("skipped.spec.csx", """
            describe("Feature", () => {
                xit("skipped spec", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_consoleOutput.ToString()).Contains("[SKIPPED]");
    }

    [Test]
    public async Task Execute_FocusedSpec_ShowsFocused()
    {
        CreateSpecFile("focused.spec.csx", """
            describe("Feature", () => {
                fit("focused spec", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_consoleOutput.ToString()).Contains("[FOCUSED]");
    }

    #endregion

    #region Filters

    [Test]
    public async Task Execute_FocusedOnlyFilter_ShowsOnlyFocused()
    {
        CreateSpecFile("mixed.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
                fit("focused spec", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, FocusedOnly = true, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("focused spec");
        await Assert.That(output).DoesNotContain("regular spec");
    }

    [Test]
    public async Task Execute_PendingOnlyFilter_ShowsOnlyPending()
    {
        CreateSpecFile("mixed.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
                it("pending spec");
            });
            """);
        var options = new CliOptions { Path = _tempDir, PendingOnly = true, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("pending spec");
        await Assert.That(output).DoesNotContain("regular spec");
    }

    [Test]
    public async Task Execute_SkippedOnlyFilter_ShowsOnlySkipped()
    {
        CreateSpecFile("mixed.spec.csx", """
            describe("Feature", () => {
                it("regular spec", () => { });
                xit("skipped spec", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, SkippedOnly = true, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("skipped spec");
        await Assert.That(output).DoesNotContain("regular spec");
    }

    [Test]
    public async Task Execute_FilterName_MatchesSubstring()
    {
        CreateSpecFile("filter.spec.csx", """
            describe("Feature", () => {
                it("add numbers", () => { });
                it("subtract numbers", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, FilterName = "add", ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("add numbers");
        await Assert.That(output).DoesNotContain("subtract");
    }

    [Test]
    public async Task Execute_FilterName_MatchesRegex()
    {
        CreateSpecFile("filter.spec.csx", """
            describe("Feature", () => {
                it("add 1", () => { });
                it("add 2", () => { });
                it("subtract 1", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, FilterName = "add \\d", ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("add 1");
        await Assert.That(output).Contains("add 2");
        await Assert.That(output).DoesNotContain("subtract");
    }

    #endregion

    #region Output Formats

    [Test]
    public async Task Execute_TreeFormat_ShowsTreeStructure()
    {
        CreateSpecFile("tree.spec.csx", """
            describe("Root", () => {
                it("spec1", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ListFormat = "tree" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        // Tree format uses box-drawing characters
        await Assert.That(output).Contains("tree.spec.csx");
        await Assert.That(output).Contains("Root");
    }

    [Test]
    public async Task Execute_FlatFormat_OneLinePerSpec()
    {
        CreateSpecFile("flat.spec.csx", """
            describe("Context", () => {
                it("spec1", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        // Flat format shows file:line and context path
        await Assert.That(output).Contains("flat.spec.csx:");
        await Assert.That(output).Contains("Context > spec1");
    }

    [Test]
    public async Task Execute_JsonFormat_ValidJson()
    {
        CreateSpecFile("json.spec.csx", """
            describe("JsonTest", () => {
                it("spec1", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ListFormat = "json" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        // JSON format contains expected structure
        await Assert.That(output).Contains("\"specs\"");
        await Assert.That(output).Contains("\"summary\"");
        await Assert.That(output).Contains("\"JsonTest\"");
    }

    [Test]
    public async Task Execute_JsonFormat_IncludesSummary()
    {
        CreateSpecFile("summary.spec.csx", """
            describe("Summary", () => {
                it("spec1", () => { });
                it("spec2", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ListFormat = "json" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("\"totalSpecs\": 2");
    }

    [Test]
    public async Task Execute_InvalidFormat_ThrowsError()
    {
        CreateSpecFile("test.spec.csx", """
            describe("Test", () => {
                it("spec", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ListFormat = "invalid" };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await ListCommand.ExecuteAsync(options));
    }

    #endregion

    #region Line Numbers

    [Test]
    public async Task Execute_ShowLineNumbers_IncludesLineNumbers()
    {
        CreateSpecFile("lines.spec.csx", """
            describe("Lines", () => {
                it("spec with line", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ShowLineNumbers = true, ListFormat = "tree" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // Line numbers appear as :N
        await Assert.That(_consoleOutput.ToString()).Contains(":"); // Contains line number indicator
    }

    [Test]
    public async Task Execute_NoLineNumbers_ExcludesLineNumbers()
    {
        CreateSpecFile("nolines.spec.csx", """
            describe("NoLines", () => {
                it("spec without line", () => { });
            });
            """);
        var options = new CliOptions { Path = _tempDir, ShowLineNumbers = false, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var output = _consoleOutput.ToString();
        // Flat format without line numbers shouldn't have :N pattern before context
        await Assert.That(output).Contains("NoLines > spec without line");
    }

    #endregion

    #region Output File

    [Test]
    public async Task Execute_OutputFile_WritesToFile()
    {
        CreateSpecFile("output.spec.csx", """
            describe("Output", () => {
                it("writes to file", () => { });
            });
            """);
        var outputFile = Path.Combine(_tempDir, "output.txt");
        var options = new CliOptions { Path = _tempDir, OutputFile = outputFile, ListFormat = "flat" };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(outputFile)).IsTrue();
        var fileContent = await File.ReadAllTextAsync(outputFile);
        await Assert.That(fileContent).Contains("Output");
    }

    [Test]
    public async Task Execute_OutputFile_PrintsConfirmation()
    {
        CreateSpecFile("confirm.spec.csx", """
            describe("Confirm", () => {
                it("spec", () => { });
            });
            """);
        var outputFile = Path.Combine(_tempDir, "confirm.txt");
        var options = new CliOptions { Path = _tempDir, OutputFile = outputFile };

        var result = await ListCommand.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_consoleOutput.ToString()).Contains("Wrote");
        await Assert.That(_consoleOutput.ToString()).Contains(outputFile);
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

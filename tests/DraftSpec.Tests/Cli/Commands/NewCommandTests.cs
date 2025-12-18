using DraftSpec.Cli;
using DraftSpec.Cli.Commands;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for NewCommand.
/// These tests modify the file system, so they run sequentially.
/// </summary>
[NotInParallel]
public class NewCommandTests
{
    private string _tempDir = null!;
    private TextWriter _originalOut = null!;
    private StringWriter _consoleOutput = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_test_{Guid.NewGuid():N}");
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

    #region Parameter Validation

    [Test]
    public async Task Execute_MissingName_ReturnsError()
    {
        var options = new CliOptions { Path = _tempDir, SpecName = null };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_consoleOutput.ToString()).Contains("Usage:");
    }

    [Test]
    public async Task Execute_EmptyName_ReturnsError()
    {
        var options = new CliOptions { Path = _tempDir, SpecName = "" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task Execute_NameWithPathSeparator_ReturnsError()
    {
        var options = new CliOptions { Path = _tempDir, SpecName = "foo/bar" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_consoleOutput.ToString()).Contains("Invalid spec name");
    }

    [Test]
    public async Task Execute_NameWithBackslash_ReturnsError()
    {
        var options = new CliOptions { Path = _tempDir, SpecName = "foo\\bar" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_consoleOutput.ToString()).Contains("Invalid spec name");
    }

    #endregion

    #region Directory Validation

    [Test]
    public async Task Execute_NonexistentDirectory_ReturnsError()
    {
        var options = new CliOptions { Path = "/nonexistent/directory", SpecName = "MySpec" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_consoleOutput.ToString()).Contains("Directory not found");
    }

    #endregion

    #region File Creation

    [Test]
    public async Task Execute_ValidName_CreatesSpecFile()
    {
        var options = new CliOptions { Path = _tempDir, SpecName = "MyFeature" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_tempDir, "MyFeature.spec.csx"))).IsTrue();
    }

    [Test]
    public async Task Execute_ValidName_SpecFileHasCorrectContent()
    {
        var options = new CliOptions { Path = _tempDir, SpecName = "UserService" };

        NewCommand.Execute(options);

        var content = File.ReadAllText(Path.Combine(_tempDir, "UserService.spec.csx"));
        await Assert.That(content).Contains("#load \"spec_helper.csx\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
        await Assert.That(content).Contains("describe(\"UserService\"");
        await Assert.That(content).Contains("run();");
    }

    [Test]
    public async Task Execute_ExistingSpec_ReturnsError()
    {
        // Create existing spec file
        File.WriteAllText(Path.Combine(_tempDir, "Existing.spec.csx"), "// existing");
        var options = new CliOptions { Path = _tempDir, SpecName = "Existing" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_consoleOutput.ToString()).Contains("already exists");
    }

    [Test]
    public async Task Execute_ExistingSpec_DoesNotOverwrite()
    {
        var specPath = Path.Combine(_tempDir, "Existing.spec.csx");
        File.WriteAllText(specPath, "// original content");
        var options = new CliOptions { Path = _tempDir, SpecName = "Existing" };

        NewCommand.Execute(options);

        var content = File.ReadAllText(specPath);
        await Assert.That(content).IsEqualTo("// original content");
    }

    #endregion

    #region Warnings

    [Test]
    public async Task Execute_NoSpecHelper_ShowsWarning()
    {
        var options = new CliOptions { Path = _tempDir, SpecName = "MySpec" };

        NewCommand.Execute(options);

        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("spec_helper.csx not found");
    }

    [Test]
    public async Task Execute_WithSpecHelper_NoWarning()
    {
        // Create spec_helper.csx
        File.WriteAllText(Path.Combine(_tempDir, "spec_helper.csx"), "// helper");
        var options = new CliOptions { Path = _tempDir, SpecName = "MySpec" };

        NewCommand.Execute(options);

        var output = _consoleOutput.ToString();
        await Assert.That(output).DoesNotContain("spec_helper.csx not found");
    }

    #endregion

    #region Console Output

    [Test]
    public async Task Execute_Success_ShowsCreatedMessage()
    {
        var options = new CliOptions { Path = _tempDir, SpecName = "MySpec" };

        NewCommand.Execute(options);

        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("Created MySpec.spec.csx");
    }

    #endregion
}

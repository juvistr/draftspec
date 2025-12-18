using DraftSpec.Cli;
using DraftSpec.Cli.Commands;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for InitCommand.
/// These tests modify the file system, so they run sequentially.
/// </summary>
[NotInParallel]
public class InitCommandTests
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

    #region Directory Validation

    [Test]
    public async Task Execute_NonexistentDirectory_ReturnsError()
    {
        var options = new CliOptions { Path = "/nonexistent/directory" };

        var result = InitCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_consoleOutput.ToString()).Contains("Directory not found");
    }

    #endregion

    #region File Creation

    [Test]
    public async Task Execute_EmptyDirectory_CreatesSpecHelper()
    {
        var options = new CliOptions { Path = _tempDir };

        var result = InitCommand.Execute(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_tempDir, "spec_helper.csx"))).IsTrue();
    }

    [Test]
    public async Task Execute_EmptyDirectory_CreatesOmnisharp()
    {
        var options = new CliOptions { Path = _tempDir };

        var result = InitCommand.Execute(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_tempDir, "omnisharp.json"))).IsTrue();
    }

    [Test]
    public async Task Execute_EmptyDirectory_SpecHelperHasDraftSpecReference()
    {
        var options = new CliOptions { Path = _tempDir };

        InitCommand.Execute(options);

        var content = File.ReadAllText(Path.Combine(_tempDir, "spec_helper.csx"));
        await Assert.That(content).Contains("#r \"nuget: DraftSpec\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
    }

    [Test]
    public async Task Execute_EmptyDirectory_OmnisharpHasScriptConfig()
    {
        var options = new CliOptions { Path = _tempDir };

        InitCommand.Execute(options);

        var content = File.ReadAllText(Path.Combine(_tempDir, "omnisharp.json"));
        await Assert.That(content).Contains("enableScriptNuGetReferences");
        await Assert.That(content).Contains("defaultTargetFramework");
    }

    #endregion

    #region Existing Files

    [Test]
    public async Task Execute_ExistingSpecHelper_DoesNotOverwrite()
    {
        var specHelperPath = Path.Combine(_tempDir, "spec_helper.csx");
        File.WriteAllText(specHelperPath, "// original content");
        var options = new CliOptions { Path = _tempDir };

        InitCommand.Execute(options);

        var content = File.ReadAllText(specHelperPath);
        await Assert.That(content).IsEqualTo("// original content");
        await Assert.That(_consoleOutput.ToString()).Contains("already exists");
    }

    [Test]
    public async Task Execute_ExistingSpecHelper_WithForce_Overwrites()
    {
        var specHelperPath = Path.Combine(_tempDir, "spec_helper.csx");
        File.WriteAllText(specHelperPath, "// original content");
        var options = new CliOptions { Path = _tempDir, Force = true };

        InitCommand.Execute(options);

        var content = File.ReadAllText(specHelperPath);
        await Assert.That(content).Contains("#r \"nuget: DraftSpec\"");
    }

    [Test]
    public async Task Execute_ExistingOmnisharp_DoesNotOverwrite()
    {
        var omnisharpPath = Path.Combine(_tempDir, "omnisharp.json");
        File.WriteAllText(omnisharpPath, "{ \"original\": true }");
        var options = new CliOptions { Path = _tempDir };

        InitCommand.Execute(options);

        var content = File.ReadAllText(omnisharpPath);
        await Assert.That(content).IsEqualTo("{ \"original\": true }");
    }

    [Test]
    public async Task Execute_ExistingOmnisharp_WithForce_Overwrites()
    {
        var omnisharpPath = Path.Combine(_tempDir, "omnisharp.json");
        File.WriteAllText(omnisharpPath, "{ \"original\": true }");
        var options = new CliOptions { Path = _tempDir, Force = true };

        InitCommand.Execute(options);

        var content = File.ReadAllText(omnisharpPath);
        await Assert.That(content).Contains("enableScriptNuGetReferences");
    }

    #endregion

    #region Console Output

    [Test]
    public async Task Execute_Success_ShowsGreenMessages()
    {
        var options = new CliOptions { Path = _tempDir };

        InitCommand.Execute(options);

        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("Created spec_helper.csx");
        await Assert.That(output).Contains("Created omnisharp.json");
    }

    [Test]
    public async Task Execute_NoCsproj_ShowsWarning()
    {
        var options = new CliOptions { Path = _tempDir };

        InitCommand.Execute(options);

        var output = _consoleOutput.ToString();
        await Assert.That(output).Contains("No .csproj found");
    }

    #endregion
}

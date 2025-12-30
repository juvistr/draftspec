using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Tests.Infrastructure.Mocks;
using MockProjectResolver = DraftSpec.Tests.TestHelpers.MockProjectResolver;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for InitCommand.
/// These tests modify the file system, so they run sequentially.
/// </summary>
[NotInParallel]
public class InitCommandTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockProjectResolver _projectResolver = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _projectResolver = new MockProjectResolver();
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private InitCommand CreateCommand() => new(_console, _fileSystem, _projectResolver);

    #region Directory Validation

    [Test]
    public async Task ExecuteAsync_NonexistentDirectory_ThrowsArgumentException()
    {
        // Don't add the directory - it won't exist
        var command = CreateCommand();
        var options = new CliOptions { Path = "/nonexistent/directory" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region File Creation

    [Test]
    public async Task ExecuteAsync_EmptyDirectory_CreatesSpecHelper()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_fileSystem.WrittenFiles.ContainsKey(Path.Combine(_tempDir, "spec_helper.csx"))).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_EmptyDirectory_CreatesOmnisharp()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_fileSystem.WrittenFiles.ContainsKey(Path.Combine(_tempDir, "omnisharp.json"))).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_EmptyDirectory_SpecHelperHasDraftSpecReference()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        await command.ExecuteAsync(options);

        var specHelperPath = Path.Combine(_tempDir, "spec_helper.csx");
        var content = _fileSystem.WrittenFiles[specHelperPath];
        await Assert.That(content).Contains("#r \"nuget: DraftSpec, *\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
    }

    [Test]
    public async Task ExecuteAsync_EmptyDirectory_OmnisharpHasScriptConfig()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        await command.ExecuteAsync(options);

        var omnisharpPath = Path.Combine(_tempDir, "omnisharp.json");
        var content = _fileSystem.WrittenFiles[omnisharpPath];
        await Assert.That(content).Contains("enableScriptNuGetReferences");
        await Assert.That(content).Contains("defaultTargetFramework");
    }

    #endregion

    #region Existing Files

    [Test]
    public async Task ExecuteAsync_ExistingSpecHelper_DoesNotOverwrite()
    {
        var specHelperPath = Path.Combine(_tempDir, "spec_helper.csx");
        _fileSystem.AddDirectory(_tempDir);
        _fileSystem.AddFile(specHelperPath, "// original content");
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        await command.ExecuteAsync(options);

        // File should still have original content, not overwritten
        await Assert.That(_fileSystem.WrittenFiles[specHelperPath]).IsEqualTo("// original content");
        await Assert.That(_console.Output).Contains("already exists");
    }

    [Test]
    public async Task ExecuteAsync_ExistingSpecHelper_WithForce_Overwrites()
    {
        var specHelperPath = Path.Combine(_tempDir, "spec_helper.csx");
        _fileSystem.AddDirectory(_tempDir);
        _fileSystem.AddFile(specHelperPath, "// original content");
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, Force = true };

        await command.ExecuteAsync(options);

        await Assert.That(_fileSystem.WrittenFiles.ContainsKey(specHelperPath)).IsTrue();
        await Assert.That(_fileSystem.WrittenFiles[specHelperPath]).Contains("#r \"nuget: DraftSpec, *\"");
    }

    [Test]
    public async Task ExecuteAsync_ExistingOmnisharp_DoesNotOverwrite()
    {
        var omnisharpPath = Path.Combine(_tempDir, "omnisharp.json");
        _fileSystem.AddDirectory(_tempDir);
        _fileSystem.AddFile(omnisharpPath, "{ \"original\": true }");
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        await command.ExecuteAsync(options);

        // File should still have original content, not overwritten
        await Assert.That(_fileSystem.WrittenFiles[omnisharpPath]).IsEqualTo("{ \"original\": true }");
    }

    [Test]
    public async Task ExecuteAsync_ExistingOmnisharp_WithForce_Overwrites()
    {
        var omnisharpPath = Path.Combine(_tempDir, "omnisharp.json");
        _fileSystem.AddDirectory(_tempDir);
        _fileSystem.AddFile(omnisharpPath, "{ \"original\": true }");
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, Force = true };

        await command.ExecuteAsync(options);

        await Assert.That(_fileSystem.WrittenFiles.ContainsKey(omnisharpPath)).IsTrue();
        await Assert.That(_fileSystem.WrittenFiles[omnisharpPath]).Contains("enableScriptNuGetReferences");
    }

    #endregion

    #region Console Output

    [Test]
    public async Task ExecuteAsync_Success_ShowsSuccessMessages()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        await command.ExecuteAsync(options);

        var output = _console.Output;
        await Assert.That(output).Contains("Created spec_helper.csx");
        await Assert.That(output).Contains("Created omnisharp.json");
    }

    [Test]
    public async Task ExecuteAsync_NoCsproj_ShowsWarning()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        await command.ExecuteAsync(options);

        var output = _console.Output;
        await Assert.That(output).Contains("No .csproj found");
    }

    #endregion
}

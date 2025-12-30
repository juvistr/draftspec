using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for NewCommand.
/// Uses mocked file system for isolation.
/// </summary>
public class NewCommandTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_test_{Guid.NewGuid():N}");
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
    }

    private NewCommand CreateCommand() => new(_console, _fileSystem);

    #region Parameter Validation

    [Test]
    public async Task ExecuteAsync_MissingName_ThrowsArgumentException()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = null };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_EmptyName_ThrowsArgumentException()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_NameWithPathSeparator_ThrowsArgumentException()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "foo/bar" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_NameWithBackslash_ThrowsArgumentException()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "foo\\bar" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region Directory Validation

    [Test]
    public async Task ExecuteAsync_NonexistentDirectory_ThrowsArgumentException()
    {
        // Don't add the directory - it won't exist
        var command = CreateCommand();
        var options = new NewOptions { Path = "/nonexistent/directory", SpecName = "MySpec" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region File Creation

    [Test]
    public async Task ExecuteAsync_ValidName_CreatesSpecFile()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "MyFeature" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var expectedPath = Path.Combine(_tempDir, "MyFeature.spec.csx");
        await Assert.That(_fileSystem.WrittenFiles.ContainsKey(expectedPath)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ValidName_SpecFileHasCorrectContent()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "UserService" };

        await command.ExecuteAsync(options);

        var specPath = Path.Combine(_tempDir, "UserService.spec.csx");
        var content = _fileSystem.WrittenFiles[specPath];
        await Assert.That(content).Contains("#load \"spec_helper.csx\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
        await Assert.That(content).Contains("describe(\"UserService\"");
    }

    [Test]
    public async Task ExecuteAsync_ExistingSpec_ThrowsArgumentException()
    {
        var specPath = Path.Combine(_tempDir, "Existing.spec.csx");
        _fileSystem.AddDirectory(_tempDir);
        _fileSystem.AddFile(specPath, "// existing");
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "Existing" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_ExistingSpec_DoesNotOverwrite()
    {
        var specPath = Path.Combine(_tempDir, "Existing.spec.csx");
        _fileSystem.AddDirectory(_tempDir);
        _fileSystem.AddFile(specPath, "// original content");
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "Existing" };

        try
        {
            await command.ExecuteAsync(options);
        }
        catch (ArgumentException)
        {
            // Expected
        }

        // File should still have original content, not overwritten
        await Assert.That(_fileSystem.WrittenFiles[specPath]).IsEqualTo("// original content");
    }

    #endregion

    #region Warnings

    [Test]
    public async Task ExecuteAsync_NoSpecHelper_ShowsWarning()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "MySpec" };

        await command.ExecuteAsync(options);

        var output = _console.Output;
        await Assert.That(output).Contains("spec_helper.csx not found");
    }

    [Test]
    public async Task ExecuteAsync_WithSpecHelper_NoWarning()
    {
        var specHelperPath = Path.Combine(_tempDir, "spec_helper.csx");
        _fileSystem.AddDirectory(_tempDir);
        _fileSystem.AddFile(specHelperPath, "// helper");
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "MySpec" };

        await command.ExecuteAsync(options);

        var output = _console.Output;
        await Assert.That(output).DoesNotContain("spec_helper.csx not found");
    }

    #endregion

    #region Console Output

    [Test]
    public async Task ExecuteAsync_Success_ShowsCreatedMessage()
    {
        _fileSystem.AddDirectory(_tempDir);
        var command = CreateCommand();
        var options = new NewOptions { Path = _tempDir, SpecName = "MySpec" };

        await command.ExecuteAsync(options);

        var output = _console.Output;
        await Assert.That(output).Contains("Created MySpec.spec.csx");
    }

    #endregion
}

using DraftSpec.Cli;
using DraftSpec.Cli.Commands;

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
        _fileSystem.DirectoryExistsResult = true;
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = null };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_EmptyName_ThrowsArgumentException()
    {
        _fileSystem.DirectoryExistsResult = true;
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_NameWithPathSeparator_ThrowsArgumentException()
    {
        _fileSystem.DirectoryExistsResult = true;
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "foo/bar" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_NameWithBackslash_ThrowsArgumentException()
    {
        _fileSystem.DirectoryExistsResult = true;
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "foo\\bar" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region Directory Validation

    [Test]
    public async Task ExecuteAsync_NonexistentDirectory_ThrowsArgumentException()
    {
        _fileSystem.DirectoryExistsResult = false;
        var command = CreateCommand();
        var options = new CliOptions { Path = "/nonexistent/directory", SpecName = "MySpec" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region File Creation

    [Test]
    public async Task ExecuteAsync_ValidName_CreatesSpecFile()
    {
        _fileSystem.DirectoryExistsResult = true;
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "MyFeature" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        var expectedPath = Path.Combine(_tempDir, "MyFeature.spec.csx");
        await Assert.That(_fileSystem.WrittenFiles.ContainsKey(expectedPath)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ValidName_SpecFileHasCorrectContent()
    {
        _fileSystem.DirectoryExistsResult = true;
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "UserService" };

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
        _fileSystem.DirectoryExistsResult = true;
        _fileSystem.ExistingFiles[specPath] = "// existing";
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "Existing" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task ExecuteAsync_ExistingSpec_DoesNotOverwrite()
    {
        var specPath = Path.Combine(_tempDir, "Existing.spec.csx");
        _fileSystem.DirectoryExistsResult = true;
        _fileSystem.ExistingFiles[specPath] = "// original content";
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "Existing" };

        try
        {
            await command.ExecuteAsync(options);
        }
        catch (ArgumentException)
        {
            // Expected
        }

        await Assert.That(_fileSystem.WrittenFiles.ContainsKey(specPath)).IsFalse();
    }

    #endregion

    #region Warnings

    [Test]
    public async Task ExecuteAsync_NoSpecHelper_ShowsWarning()
    {
        _fileSystem.DirectoryExistsResult = true;
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "MySpec" };

        await command.ExecuteAsync(options);

        var output = _console.Output;
        await Assert.That(output).Contains("spec_helper.csx not found");
    }

    [Test]
    public async Task ExecuteAsync_WithSpecHelper_NoWarning()
    {
        var specHelperPath = Path.Combine(_tempDir, "spec_helper.csx");
        _fileSystem.DirectoryExistsResult = true;
        _fileSystem.ExistingFiles[specHelperPath] = "// helper";
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "MySpec" };

        await command.ExecuteAsync(options);

        var output = _console.Output;
        await Assert.That(output).DoesNotContain("spec_helper.csx not found");
    }

    #endregion

    #region Console Output

    [Test]
    public async Task ExecuteAsync_Success_ShowsCreatedMessage()
    {
        _fileSystem.DirectoryExistsResult = true;
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, SpecName = "MySpec" };

        await command.ExecuteAsync(options);

        var output = _console.Output;
        await Assert.That(output).Contains("Created MySpec.spec.csx");
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

    private class MockFileSystem : IFileSystem
    {
        public Dictionary<string, string> ExistingFiles { get; } = new();
        public Dictionary<string, string> WrittenFiles { get; } = new();
        public bool DirectoryExistsResult { get; set; } = true;

        public bool FileExists(string path) => ExistingFiles.ContainsKey(path);
        public void WriteAllText(string path, string content) => WrittenFiles[path] = content;
        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        {
            WrittenFiles[path] = content;
            return Task.CompletedTask;
        }
        public string ReadAllText(string path) => ExistingFiles.TryGetValue(path, out var content) ? content : "";
        public bool DirectoryExists(string path) => DirectoryExistsResult;
        public void CreateDirectory(string path) { }
    }

    #endregion
}

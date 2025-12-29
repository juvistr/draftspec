using DraftSpec.Cli;
using DraftSpec.Cli.Commands;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for ValidateCommand.
/// These tests use the real file system for spec validation.
/// </summary>
[NotInParallel]
public class ValidateCommandTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;
    private RealFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_validate_test_{Guid.NewGuid():N}");
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

    private ValidateCommand CreateCommand() => new(_console, _fileSystem);

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
    public async Task ExecuteAsync_EmptyDirectory_ReturnsSuccess()
    {
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
        await Assert.That(_console.Output).Contains("No spec files found");
    }

    #endregion

    #region Valid Specs

    [Test]
    public async Task ExecuteAsync_ValidSpecs_ReturnsSuccess()
    {
        CreateSpecFile("valid.spec.csx", """
            describe("Calculator", () => {
                it("adds numbers", () => { });
                it("subtracts numbers", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
        await Assert.That(_console.Output).Contains("\u2713"); // checkmark
        await Assert.That(_console.Output).Contains("valid.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_MultipleValidFiles_ReturnsSuccess()
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

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
        await Assert.That(_console.Output).Contains("math.spec.csx");
        await Assert.That(_console.Output).Contains("string.spec.csx");
    }

    #endregion

    #region Warnings

    [Test]
    public async Task ExecuteAsync_DynamicDescription_ReturnsSuccessWithWarnings()
    {
        // Dynamic descriptions generate warnings, not errors
        CreateSpecFile("dynamic.spec.csx", """
            var name = "test";
            describe("Feature", () => {
                it($"dynamic {name}", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        // Warnings don't cause failure by default
        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
    }

    [Test]
    public async Task ExecuteAsync_WarningsWithStrict_ReturnsExitWarnings()
    {
        CreateSpecFile("dynamic.spec.csx", """
            var name = "test";
            describe("Feature", () => {
                it($"dynamic {name}", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, Strict = true };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitWarnings);
    }

    #endregion

    #region Errors

    [Test]
    public async Task ExecuteAsync_MissingDescription_ReturnsExitErrors()
    {
        // it() with no arguments triggers "missing description argument" error
        CreateSpecFile("missing.spec.csx", """
            describe("Feature", () => {
                it();
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitErrors);
        await Assert.That(_console.ErrorOutput).Contains("missing description");
    }

    #endregion

    #region Quiet Mode

    [Test]
    public async Task ExecuteAsync_Quiet_SuppressesNonErrors()
    {
        CreateSpecFile("valid.spec.csx", """
            describe("Feature", () => {
                it("spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, Quiet = true };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
        // In quiet mode, valid files shouldn't produce output
        await Assert.That(_console.Output).DoesNotContain("\u2713");
    }

    [Test]
    public async Task ExecuteAsync_QuietWithErrors_ShowsErrors()
    {
        // it() with no arguments triggers "missing description argument" error
        CreateSpecFile("missing.spec.csx", """
            describe("Feature", () => {
                it();
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir, Quiet = true };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitErrors);
        // Errors should still be shown even in quiet mode
        await Assert.That(_console.ErrorOutput).Contains("missing.spec.csx");
    }

    #endregion

    #region Files Flag

    [Test]
    public async Task ExecuteAsync_FilesFlag_ValidatesOnlySpecifiedFiles()
    {
        CreateSpecFile("good.spec.csx", """
            describe("Good", () => {
                it("valid", () => { });
            });
            """);
        CreateSpecFile("bad.spec.csx", """
            describe("Bad", () => {
                it(() => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions
        {
            Path = _tempDir,
            Files = ["good.spec.csx"]
        };

        var result = await command.ExecuteAsync(options);

        // Only good.spec.csx is validated, bad.spec.csx is ignored
        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
        await Assert.That(_console.Output).Contains("good.spec.csx");
        await Assert.That(_console.Output).DoesNotContain("bad.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_FilesFlag_MultipleFiles()
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
        var command = CreateCommand();
        var options = new CliOptions
        {
            Path = _tempDir,
            Files = ["a.spec.csx", "b.spec.csx"]
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
        await Assert.That(_console.Output).Contains("a.spec.csx");
        await Assert.That(_console.Output).Contains("b.spec.csx");
        await Assert.That(_console.Output).DoesNotContain("c.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_FilesFlag_NonexistentFile_IgnoresIt()
    {
        CreateSpecFile("exists.spec.csx", """
            describe("Exists", () => {
                it("spec", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions
        {
            Path = _tempDir,
            Files = ["exists.spec.csx", "nonexistent.spec.csx"]
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
        await Assert.That(_console.Output).Contains("exists.spec.csx");
    }

    #endregion

    #region Summary Output

    [Test]
    public async Task ExecuteAsync_ShowsSummary()
    {
        CreateSpecFile("summary.spec.csx", """
            describe("Summary", () => {
                it("spec1", () => { });
                it("spec2", () => { });
            });
            """);
        var command = CreateCommand();
        var options = new CliOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(ValidateCommand.ExitSuccess);
        await Assert.That(_console.Output).Contains("Files:");
        await Assert.That(_console.Output).Contains("Specs:");
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
        private readonly List<string> _errorOutput = [];

        public string Output => string.Join("", _output);
        public string ErrorOutput => string.Join("", _errorOutput);

        public void Write(string text) => _output.Add(text);
        public void WriteLine(string text) => _output.Add(text + "\n");
        public void WriteLine() => _output.Add("\n");
        public ConsoleColor ForegroundColor { get; set; }
        public void ResetColor() { }
        public void Clear() { }
        public void WriteWarning(string text) => WriteLine(text);
        public void WriteSuccess(string text) => WriteLine(text);
        public void WriteError(string text)
        {
            _errorOutput.Add(text + "\n");
            _output.Add(text + "\n");
        }
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

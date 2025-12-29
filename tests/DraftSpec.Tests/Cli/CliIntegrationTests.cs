using DraftSpec.Cli;
using DraftSpec.Cli.Commands;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Integration tests for CLI commands.
/// </summary>
public class CliIntegrationTests
{
    private string _testDirectory = null!;
    private MockConsole _console = null!;
    private RealFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CliIntegrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _console = new MockConsole();
        _fileSystem = new RealFileSystem();
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
    }

    private InitCommand CreateInitCommand() => new(_console, _fileSystem);
    private NewCommand CreateNewCommand() => new(_console, _fileSystem);

    #region InitCommand Tests

    [Test]
    public async Task InitCommand_CreatesSpecHelper()
    {
        var command = CreateInitCommand();
        var options = new CliOptions { Path = _testDirectory };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "spec_helper.csx"))).IsTrue();
    }

    [Test]
    public async Task InitCommand_CreatesOmnisharp()
    {
        var command = CreateInitCommand();
        var options = new CliOptions { Path = _testDirectory };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "omnisharp.json"))).IsTrue();
    }

    [Test]
    public async Task InitCommand_SpecHelperContainsDraftSpecReference()
    {
        var command = CreateInitCommand();
        var options = new CliOptions { Path = _testDirectory };

        await command.ExecuteAsync(options);

        var content = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "spec_helper.csx"));
        await Assert.That(content).Contains("#r \"nuget: DraftSpec\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
    }

    [Test]
    public async Task InitCommand_OmnisharpContainsScriptConfig()
    {
        var command = CreateInitCommand();
        var options = new CliOptions { Path = _testDirectory };

        await command.ExecuteAsync(options);

        var content = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "omnisharp.json"));
        await Assert.That(content).Contains("\"enableScriptNuGetReferences\": true");
    }

    [Test]
    public async Task InitCommand_SkipsExistingWithoutForce()
    {
        var specHelperPath = Path.Combine(_testDirectory, "spec_helper.csx");
        await File.WriteAllTextAsync(specHelperPath, "// existing");

        var command = CreateInitCommand();
        var options = new CliOptions { Path = _testDirectory, Force = false };
        await command.ExecuteAsync(options);

        var content = await File.ReadAllTextAsync(specHelperPath);
        await Assert.That(content).IsEqualTo("// existing");
    }

    [Test]
    public async Task InitCommand_OverwritesWithForce()
    {
        var specHelperPath = Path.Combine(_testDirectory, "spec_helper.csx");
        await File.WriteAllTextAsync(specHelperPath, "// existing");

        var command = CreateInitCommand();
        var options = new CliOptions { Path = _testDirectory, Force = true };
        await command.ExecuteAsync(options);

        var content = await File.ReadAllTextAsync(specHelperPath);
        await Assert.That(content).Contains("#r \"nuget: DraftSpec\"");
    }

    [Test]
    public async Task InitCommand_InvalidDirectory_ThrowsArgumentException()
    {
        var command = CreateInitCommand();
        var options = new CliOptions { Path = "/nonexistent/path" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region NewCommand Tests

    [Test]
    public async Task NewCommand_CreatesSpecFile()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = "MyFeature" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "MyFeature.spec.csx"))).IsTrue();
    }

    [Test]
    public async Task NewCommand_SpecFileContainsDescribe()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = "Calculator" };

        await command.ExecuteAsync(options);

        var content = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "Calculator.spec.csx"));
        await Assert.That(content).Contains("#load \"spec_helper.csx\"");
        await Assert.That(content).Contains("describe(\"Calculator\"");
    }

    [Test]
    public async Task NewCommand_NoName_ThrowsArgumentException()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = null };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task NewCommand_EmptyName_ThrowsArgumentException()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = "" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task NewCommand_FileExists_ThrowsArgumentException()
    {
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "Existing.spec.csx"), "// existing");

        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = "Existing" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task NewCommand_InvalidDirectory_ThrowsArgumentException()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = "/nonexistent/path", SpecName = "Test" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region Security Tests - Path Traversal Prevention

    [Test]
    public async Task NewCommand_NameWithPathSeparator_ThrowsArgumentException()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = "../../../etc/malicious" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task NewCommand_NameWithBackslash_ThrowsArgumentException()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = "..\\..\\malicious" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task NewCommand_NameWithDoubleDot_ThrowsArgumentException()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = ".." };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    [Test]
    public async Task NewCommand_NameStartingWithDoubleDot_ThrowsArgumentException()
    {
        var command = CreateNewCommand();
        var options = new CliOptions { Path = _testDirectory, SpecName = "..foo" };

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region SpecFinder Tests

    [Test]
    public async Task SpecFinder_FindsSingleSpecFile()
    {
        var specPath = Path.Combine(_testDirectory, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, "// spec");

        var finder = new SpecFinder();
        var specs = finder.FindSpecs(specPath, _testDirectory);

        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0]).IsEqualTo(specPath);
    }

    [Test]
    public async Task SpecFinder_FindsAllSpecsInDirectory()
    {
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "one.spec.csx"), "// spec");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "two.spec.csx"), "// spec");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "other.cs"), "// not a spec");

        var finder = new SpecFinder();
        var specs = finder.FindSpecs(_testDirectory, _testDirectory);

        await Assert.That(specs).Count().IsEqualTo(2);
    }

    [Test]
    public async Task SpecFinder_FindsSpecsInSubdirectories()
    {
        var subDir = Path.Combine(_testDirectory, "Specs");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.spec.csx"), "// spec");

        var finder = new SpecFinder();
        var specs = finder.FindSpecs(_testDirectory, _testDirectory);

        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0]).Contains("nested.spec.csx");
    }

    [Test]
    public async Task SpecFinder_ReturnsSortedResults()
    {
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "z.spec.csx"), "// spec");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "a.spec.csx"), "// spec");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "m.spec.csx"), "// spec");

        var finder = new SpecFinder();
        var specs = finder.FindSpecs(_testDirectory, _testDirectory);

        await Assert.That(specs[0]).Contains("a.spec.csx");
        await Assert.That(specs[1]).Contains("m.spec.csx");
        await Assert.That(specs[2]).Contains("z.spec.csx");
    }

    [Test]
    public async Task SpecFinder_NonSpecFile_ThrowsArgumentException()
    {
        var regularFile = Path.Combine(_testDirectory, "test.cs");
        await File.WriteAllTextAsync(regularFile, "// not a spec");

        var finder = new SpecFinder();

        await Assert.That(() => finder.FindSpecs(regularFile, _testDirectory))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task SpecFinder_EmptyDirectory_ThrowsArgumentException()
    {
        var finder = new SpecFinder();

        await Assert.That(() => finder.FindSpecs(_testDirectory, _testDirectory))
            .Throws<ArgumentException>();
    }

    #endregion

    #region CliOptions Tests

    [Test]
    public async Task CliOptions_DefaultPath_IsDot()
    {
        var options = new CliOptions();

        await Assert.That(options.Path).IsEqualTo(".");
    }

    [Test]
    public async Task CliOptions_DefaultFormat_IsConsole()
    {
        var options = new CliOptions();

        await Assert.That(options.Format).IsEqualTo("console");
    }

    [Test]
    public async Task CliOptions_DefaultParallel_IsFalse()
    {
        var options = new CliOptions();

        await Assert.That(options.Parallel).IsFalse();
    }

    [Test]
    public async Task CliOptions_DefaultForce_IsFalse()
    {
        var options = new CliOptions();

        await Assert.That(options.Force).IsFalse();
    }

    #endregion

    #region OutputFormats Tests

    [Test]
    public async Task OutputFormats_ContainsExpectedFormats()
    {
        await Assert.That(OutputFormats.Console).IsEqualTo("console");
        await Assert.That(OutputFormats.Json).IsEqualTo("json");
        await Assert.That(OutputFormats.Markdown).IsEqualTo("markdown");
        await Assert.That(OutputFormats.Html).IsEqualTo("html");
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

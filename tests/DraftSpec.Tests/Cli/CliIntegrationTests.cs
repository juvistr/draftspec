using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Common;
using DraftSpec.Cli.Pipeline.Phases.Init;
using DraftSpec.Cli.Pipeline.Phases.NewSpec;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Integration tests for CLI commands.
/// </summary>
public class CliIntegrationTests
{
    private string _testDirectory = null!;
    private MockConsole _console = null!;
    private FileSystem _fileSystem = null!;
    private ProjectResolver _projectResolver = null!;

    [Before(Test)]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CliIntegrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _console = new MockConsole();
        _fileSystem = new FileSystem();
        _projectResolver = new ProjectResolver();
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
    }

    private InitCommand CreateInitCommand()
    {
        var pipeline = new CommandPipelineBuilder()
            .Use(new PathResolutionPhase())
            .Use(new ProjectDiscoveryPhase(_projectResolver))
            .Use(new InitOutputPhase())
            .Build();
        return new InitCommand(pipeline, _console, _fileSystem);
    }

    private NewCommand CreateNewCommand()
    {
        var pipeline = new CommandPipelineBuilder()
            .Use(new PathResolutionPhase())
            .Use(new NewSpecOutputPhase())
            .Build();
        return new NewCommand(pipeline, _console, _fileSystem);
    }

    #region InitCommand Tests

    [Test]
    public async Task InitCommand_CreatesSpecHelper()
    {
        var command = CreateInitCommand();
        var options = new InitOptions { Path = _testDirectory };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "spec_helper.csx"))).IsTrue();
    }

    [Test]
    public async Task InitCommand_CreatesOmnisharp()
    {
        var command = CreateInitCommand();
        var options = new InitOptions { Path = _testDirectory };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "omnisharp.json"))).IsTrue();
    }

    [Test]
    public async Task InitCommand_SpecHelperContainsDraftSpecReference()
    {
        var command = CreateInitCommand();
        var options = new InitOptions { Path = _testDirectory };

        await command.ExecuteAsync(options);

        var content = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "spec_helper.csx"));
        await Assert.That(content).Contains("#r \"nuget: DraftSpec, *\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
    }

    [Test]
    public async Task InitCommand_OmnisharpContainsScriptConfig()
    {
        var command = CreateInitCommand();
        var options = new InitOptions { Path = _testDirectory };

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
        var options = new InitOptions { Path = _testDirectory, Force = false };
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
        var options = new InitOptions { Path = _testDirectory, Force = true };
        await command.ExecuteAsync(options);

        var content = await File.ReadAllTextAsync(specHelperPath);
        await Assert.That(content).Contains("#r \"nuget: DraftSpec, *\"");
    }

    [Test]
    public async Task InitCommand_InvalidDirectory_ReturnsError()
    {
        var command = CreateInitCommand();
        var options = new InitOptions { Path = "/nonexistent/path" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("not found");
    }

    #endregion

    #region NewCommand Tests

    [Test]
    public async Task NewCommand_CreatesSpecFile()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = "MyFeature" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "MyFeature.spec.csx"))).IsTrue();
    }

    [Test]
    public async Task NewCommand_SpecFileContainsDescribe()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = "Calculator" };

        await command.ExecuteAsync(options);

        var content = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "Calculator.spec.csx"));
        await Assert.That(content).Contains("#load \"spec_helper.csx\"");
        await Assert.That(content).Contains("describe(\"Calculator\"");
    }

    [Test]
    public async Task NewCommand_NoName_ReturnsError()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = null };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task NewCommand_EmptyName_ReturnsError()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = "" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task NewCommand_FileExists_ReturnsError()
    {
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "Existing.spec.csx"), "// existing");

        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = "Existing" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("already exists");
    }

    [Test]
    public async Task NewCommand_InvalidDirectory_ReturnsError()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = "/nonexistent/path", SpecName = "Test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("not found");
    }

    #endregion

    #region Security Tests - Path Traversal Prevention

    [Test]
    public async Task NewCommand_NameWithPathSeparator_ReturnsError()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = "../../../etc/malicious" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("path separator");
    }

    [Test]
    public async Task NewCommand_NameWithBackslash_ReturnsError()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = "..\\..\\malicious" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("path separator");
    }

    [Test]
    public async Task NewCommand_NameWithDoubleDot_ReturnsError()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = ".." };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("relative path reference");
    }

    [Test]
    public async Task NewCommand_NameStartingWithDoubleDot_ReturnsError()
    {
        var command = CreateNewCommand();
        var options = new NewOptions { Path = _testDirectory, SpecName = "..foo" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("relative path reference");
    }

    #endregion

    #region SpecFinder Tests

    [Test]
    public async Task SpecFinder_FindsSingleSpecFile()
    {
        var specPath = Path.Combine(_testDirectory, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, "// spec");

        var finder = new SpecFinder(new FileSystem());
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

        var finder = new SpecFinder(new FileSystem());
        var specs = finder.FindSpecs(_testDirectory, _testDirectory);

        await Assert.That(specs).Count().IsEqualTo(2);
    }

    [Test]
    public async Task SpecFinder_FindsSpecsInSubdirectories()
    {
        var subDir = Path.Combine(_testDirectory, "Specs");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.spec.csx"), "// spec");

        var finder = new SpecFinder(new FileSystem());
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

        var finder = new SpecFinder(new FileSystem());
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

        var finder = new SpecFinder(new FileSystem());

        await Assert.That(() => finder.FindSpecs(regularFile, _testDirectory))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task SpecFinder_EmptyDirectory_ReturnsEmptyList()
    {
        var finder = new SpecFinder(new FileSystem());

        var result = finder.FindSpecs(_testDirectory, _testDirectory);

        await Assert.That(result).IsEmpty();
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

        await Assert.That(options.Format).IsEqualTo(OutputFormat.Console);
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
}

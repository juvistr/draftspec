using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Formatters.Html;
using DraftSpec.Formatters.Markdown;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Integration tests for CLI commands.
/// </summary>
public class CliIntegrationTests
{
    private string _testDirectory = null!;

    [Before(Test)]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CliIntegrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
    }

    #region InitCommand Tests

    [Test]
    public async Task InitCommand_CreatesSpecHelper()
    {
        var options = new CliOptions { Path = _testDirectory };

        var result = InitCommand.Execute(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "spec_helper.csx"))).IsTrue();
    }

    [Test]
    public async Task InitCommand_CreatesOmnisharp()
    {
        var options = new CliOptions { Path = _testDirectory };

        var result = InitCommand.Execute(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "omnisharp.json"))).IsTrue();
    }

    [Test]
    public async Task InitCommand_SpecHelperContainsDraftSpecReference()
    {
        var options = new CliOptions { Path = _testDirectory };

        InitCommand.Execute(options);

        var content = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "spec_helper.csx"));
        await Assert.That(content).Contains("#r \"nuget: DraftSpec\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
    }

    [Test]
    public async Task InitCommand_OmnisharpContainsScriptConfig()
    {
        var options = new CliOptions { Path = _testDirectory };

        InitCommand.Execute(options);

        var content = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "omnisharp.json"));
        await Assert.That(content).Contains("\"enableScriptNuGetReferences\": true");
    }

    [Test]
    public async Task InitCommand_SkipsExistingWithoutForce()
    {
        var specHelperPath = Path.Combine(_testDirectory, "spec_helper.csx");
        await File.WriteAllTextAsync(specHelperPath, "// existing");

        var options = new CliOptions { Path = _testDirectory, Force = false };
        InitCommand.Execute(options);

        var content = await File.ReadAllTextAsync(specHelperPath);
        await Assert.That(content).IsEqualTo("// existing");
    }

    [Test]
    public async Task InitCommand_OverwritesWithForce()
    {
        var specHelperPath = Path.Combine(_testDirectory, "spec_helper.csx");
        await File.WriteAllTextAsync(specHelperPath, "// existing");

        var options = new CliOptions { Path = _testDirectory, Force = true };
        InitCommand.Execute(options);

        var content = await File.ReadAllTextAsync(specHelperPath);
        await Assert.That(content).Contains("#r \"nuget: DraftSpec\"");
    }

    [Test]
    public async Task InitCommand_InvalidDirectory_ReturnsError()
    {
        var options = new CliOptions { Path = "/nonexistent/path" };

        var result = InitCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
    }

    #endregion

    #region NewCommand Tests

    [Test]
    public async Task NewCommand_CreatesSpecFile()
    {
        var options = new CliOptions { Path = _testDirectory, SpecName = "MyFeature" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "MyFeature.spec.csx"))).IsTrue();
    }

    [Test]
    public async Task NewCommand_SpecFileContainsDescribe()
    {
        var options = new CliOptions { Path = _testDirectory, SpecName = "Calculator" };

        NewCommand.Execute(options);

        var content = await File.ReadAllTextAsync(Path.Combine(_testDirectory, "Calculator.spec.csx"));
        await Assert.That(content).Contains("#load \"spec_helper.csx\"");
        await Assert.That(content).Contains("describe(\"Calculator\"");
        await Assert.That(content).Contains("run();");
    }

    [Test]
    public async Task NewCommand_NoName_ReturnsError()
    {
        var options = new CliOptions { Path = _testDirectory, SpecName = null };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task NewCommand_EmptyName_ReturnsError()
    {
        var options = new CliOptions { Path = _testDirectory, SpecName = "" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task NewCommand_FileExists_ReturnsError()
    {
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "Existing.spec.csx"), "// existing");

        var options = new CliOptions { Path = _testDirectory, SpecName = "Existing" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task NewCommand_InvalidDirectory_ReturnsError()
    {
        var options = new CliOptions { Path = "/nonexistent/path", SpecName = "Test" };

        var result = NewCommand.Execute(options);

        await Assert.That(result).IsEqualTo(1);
    }

    #endregion

    #region RunCommand.GetFormatter Tests

    [Test]
    public async Task GetFormatter_Json_ReturnsJsonFormatter()
    {
        var formatter = RunCommand.GetFormatter("json", new CliOptions());

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter!.FileExtension).IsEqualTo(".json");
    }

    [Test]
    public async Task GetFormatter_Markdown_ReturnsMarkdownFormatter()
    {
        var formatter = RunCommand.GetFormatter("markdown", new CliOptions());

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<MarkdownFormatter>();
    }

    [Test]
    public async Task GetFormatter_Html_ReturnsHtmlFormatter()
    {
        var formatter = RunCommand.GetFormatter("html", new CliOptions());

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<HtmlFormatter>();
    }

    [Test]
    public async Task GetFormatter_HtmlWithCustomCss_UsesCustomUrl()
    {
        var options = new CliOptions { CssUrl = "https://custom.css" };

        var formatter = RunCommand.GetFormatter("html", options) as HtmlFormatter;

        await Assert.That(formatter).IsNotNull();
        // HtmlFormatter would use the custom CSS URL
    }

    [Test]
    public async Task GetFormatter_Unknown_ReturnsNull()
    {
        var formatter = RunCommand.GetFormatter("unknown", new CliOptions());

        await Assert.That(formatter).IsNull();
    }

    [Test]
    public async Task GetFormatter_CaseInsensitive()
    {
        var jsonLower = RunCommand.GetFormatter("json", new CliOptions());
        var jsonUpper = RunCommand.GetFormatter("JSON", new CliOptions());
        var jsonMixed = RunCommand.GetFormatter("Json", new CliOptions());

        await Assert.That(jsonLower).IsNotNull();
        await Assert.That(jsonUpper).IsNotNull();
        await Assert.That(jsonMixed).IsNotNull();
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
}
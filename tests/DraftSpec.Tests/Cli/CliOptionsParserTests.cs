using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for CLI argument parsing.
/// </summary>
public class CliOptionsParserTests
{
    #region Commands

    [Test]
    public async Task Parse_RunCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.Command).IsEqualTo("run");
        await Assert.That(options.Path).IsEqualTo(".");
    }

    [Test]
    public async Task Parse_WatchCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["watch", "./specs"]);

        await Assert.That(options.Command).IsEqualTo("watch");
        await Assert.That(options.Path).IsEqualTo("./specs");
    }

    [Test]
    public async Task Parse_InitCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["init"]);

        await Assert.That(options.Command).IsEqualTo("init");
    }

    [Test]
    public async Task Parse_NewCommand_SetsSpecName()
    {
        var options = CliOptionsParser.Parse(["new", "Calculator"]);

        await Assert.That(options.Command).IsEqualTo("new");
        await Assert.That(options.SpecName).IsEqualTo("Calculator");
    }

    [Test]
    public async Task Parse_NewCommandWithPath_SetsSpecNameAndPath()
    {
        var options = CliOptionsParser.Parse(["new", "Calculator", "./specs"]);

        await Assert.That(options.Command).IsEqualTo("new");
        await Assert.That(options.SpecName).IsEqualTo("Calculator");
        await Assert.That(options.Path).IsEqualTo("./specs");
    }

    [Test]
    public async Task Parse_CommandIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["RUN", "."]);

        await Assert.That(options.Command).IsEqualTo("run");
    }

    #endregion

    #region Help

    [Test]
    public async Task Parse_HelpFlag_SetsShowHelp()
    {
        var options = CliOptionsParser.Parse(["--help"]);

        await Assert.That(options.ShowHelp).IsTrue();
    }

    [Test]
    public async Task Parse_ShortHelpFlag_SetsShowHelp()
    {
        var options = CliOptionsParser.Parse(["-h"]);

        await Assert.That(options.ShowHelp).IsTrue();
    }

    [Test]
    public async Task Parse_HelpCommand_SetsShowHelp()
    {
        var options = CliOptionsParser.Parse(["help"]);

        await Assert.That(options.ShowHelp).IsTrue();
    }

    #endregion

    #region Format Option

    [Test]
    public async Task Parse_FormatOption_SetsFormat()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--format", "json"]);

        await Assert.That(options.Format).IsEqualTo("json");
    }

    [Test]
    public async Task Parse_ShortFormatOption_SetsFormat()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-f", "markdown"]);

        await Assert.That(options.Format).IsEqualTo("markdown");
    }

    [Test]
    public async Task Parse_FormatIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-f", "JSON"]);

        await Assert.That(options.Format).IsEqualTo("json");
    }

    [Test]
    public async Task Parse_FormatWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--format"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--format requires a value");
    }

    #endregion

    #region Output Option

    [Test]
    public async Task Parse_OutputOption_SetsOutputFile()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--output", "report.json"]);

        await Assert.That(options.OutputFile).IsEqualTo("report.json");
    }

    [Test]
    public async Task Parse_ShortOutputOption_SetsOutputFile()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-o", "report.md"]);

        await Assert.That(options.OutputFile).IsEqualTo("report.md");
    }

    [Test]
    public async Task Parse_OutputWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-o"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--output requires a file path");
    }

    #endregion

    #region Other Options

    [Test]
    public async Task Parse_CssUrlOption_SetsCssUrl()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--css-url", "https://example.com/style.css"]);

        await Assert.That(options.CssUrl).IsEqualTo("https://example.com/style.css");
    }

    [Test]
    public async Task Parse_CssUrlWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--css-url"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--css-url requires a URL");
    }

    [Test]
    public async Task Parse_ForceFlag_SetsForce()
    {
        var options = CliOptionsParser.Parse(["init", "--force"]);

        await Assert.That(options.Force).IsTrue();
    }

    [Test]
    public async Task Parse_ParallelFlag_SetsParallel()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--parallel"]);

        await Assert.That(options.Parallel).IsTrue();
    }

    [Test]
    public async Task Parse_ShortParallelFlag_SetsParallel()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-p"]);

        await Assert.That(options.Parallel).IsTrue();
    }

    [Test]
    public async Task Parse_NoCacheFlag_SetsNoCache()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--no-cache"]);

        await Assert.That(options.NoCache).IsTrue();
    }

    #endregion

    #region Error Cases

    [Test]
    public async Task Parse_UnknownOption_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--unknown"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("Unknown option: --unknown");
    }

    [Test]
    public async Task Parse_EmptyArgs_ReturnsDefaultOptions()
    {
        var options = CliOptionsParser.Parse([]);

        // Command stays as default empty string when no args provided
        await Assert.That(options.Command).IsEqualTo("");
        await Assert.That(options.Error).IsNull();
    }

    #endregion

    #region Combined Options

    [Test]
    public async Task Parse_MultipleOptions_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "run", "./specs",
            "-f", "html",
            "-o", "report.html",
            "--css-url", "https://example.com/style.css",
            "--parallel"
        ]);

        await Assert.That(options.Command).IsEqualTo("run");
        await Assert.That(options.Path).IsEqualTo("./specs");
        await Assert.That(options.Format).IsEqualTo("html");
        await Assert.That(options.OutputFile).IsEqualTo("report.html");
        await Assert.That(options.CssUrl).IsEqualTo("https://example.com/style.css");
        await Assert.That(options.Parallel).IsTrue();
    }

    #endregion
}

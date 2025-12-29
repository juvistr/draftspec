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

    [Test]
    public async Task Parse_BailFlag_SetsBail()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--bail"]);

        await Assert.That(options.Bail).IsTrue();
    }

    [Test]
    public async Task Parse_ShortBailFlag_SetsBail()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-b"]);

        await Assert.That(options.Bail).IsTrue();
    }

    [Test]
    public async Task Parse_BailDefaultIsFalse()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.Bail).IsFalse();
    }

    #endregion

    #region Filter Options

    [Test]
    public async Task Parse_FilterTagsOption_SetsFilterTags()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--filter-tags", "fast,unit"]);

        await Assert.That(options.FilterTags).IsEqualTo("fast,unit");
    }

    [Test]
    public async Task Parse_ShortFilterTagsOption_SetsFilterTags()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-t", "integration"]);

        await Assert.That(options.FilterTags).IsEqualTo("integration");
    }

    [Test]
    public async Task Parse_FilterTagsWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--filter-tags"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--filter-tags requires a value");
    }

    [Test]
    public async Task Parse_ExcludeTagsOption_SetsExcludeTags()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--exclude-tags", "slow,flaky"]);

        await Assert.That(options.ExcludeTags).IsEqualTo("slow,flaky");
    }

    [Test]
    public async Task Parse_ShortExcludeTagsOption_SetsExcludeTags()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-x", "wip"]);

        await Assert.That(options.ExcludeTags).IsEqualTo("wip");
    }

    [Test]
    public async Task Parse_ExcludeTagsWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--exclude-tags"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--exclude-tags requires a value");
    }

    [Test]
    public async Task Parse_FilterNameOption_SetsFilterName()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--filter-name", "Calculator.*add"]);

        await Assert.That(options.FilterName).IsEqualTo("Calculator.*add");
    }

    [Test]
    public async Task Parse_ShortFilterNameOption_SetsFilterName()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-n", "should.*return"]);

        await Assert.That(options.FilterName).IsEqualTo("should.*return");
    }

    [Test]
    public async Task Parse_FilterNameWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--filter-name"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--filter-name requires a value");
    }

    [Test]
    public async Task Parse_ExcludeNameOption_SetsExcludeName()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--exclude-name", ".*slow.*"]);

        await Assert.That(options.ExcludeName).IsEqualTo(".*slow.*");
    }

    [Test]
    public async Task Parse_ExcludeNameWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--exclude-name"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--exclude-name requires a value");
    }

    [Test]
    public async Task Parse_FilterOptionsDefaultsAreNull()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.FilterTags).IsNull();
        await Assert.That(options.ExcludeTags).IsNull();
        await Assert.That(options.FilterName).IsNull();
        await Assert.That(options.ExcludeName).IsNull();
    }

    [Test]
    public async Task Parse_CombinedFilterOptions_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "run", ".",
            "-t", "fast,unit",
            "-x", "slow",
            "-n", "Calculator",
            "--exclude-name", ".*deprecated.*"
        ]);

        await Assert.That(options.FilterTags).IsEqualTo("fast,unit");
        await Assert.That(options.ExcludeTags).IsEqualTo("slow");
        await Assert.That(options.FilterName).IsEqualTo("Calculator");
        await Assert.That(options.ExcludeName).IsEqualTo(".*deprecated.*");
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

    #region Coverage Options

    [Test]
    public async Task Parse_CoverageFlag_SetsCoverage()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage"]);

        await Assert.That(options.Coverage).IsTrue();
    }

    [Test]
    public async Task Parse_CoverageDefaultIsFalse()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.Coverage).IsFalse();
    }

    [Test]
    public async Task Parse_CoverageOutputOption_SetsCoverageOutput()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage", "--coverage-output", "./reports"]);

        await Assert.That(options.CoverageOutput).IsEqualTo("./reports");
    }

    [Test]
    public async Task Parse_CoverageOutputWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage-output"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--coverage-output requires a directory path");
    }

    [Test]
    public async Task Parse_CoverageFormatOption_SetsCoverageFormat()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage", "--coverage-format", "xml"]);

        await Assert.That(options.CoverageFormat).IsEqualTo("xml");
    }

    [Test]
    public async Task Parse_CoverageFormatWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage-format"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--coverage-format requires a value");
    }

    [Test]
    public async Task Parse_CoverageFormatDefaultIsCobertura()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage"]);

        await Assert.That(options.CoverageFormat).IsEqualTo("cobertura");
    }

    [Test]
    public async Task Parse_CoverageFormatIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage-format", "COBERTURA"]);

        await Assert.That(options.CoverageFormat).IsEqualTo("cobertura");
    }

    [Test]
    public async Task Parse_AllCoverageOptions_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "run", ".",
            "--coverage",
            "--coverage-output", "./coverage-reports",
            "--coverage-format", "xml"
        ]);

        await Assert.That(options.Coverage).IsTrue();
        await Assert.That(options.CoverageOutput).IsEqualTo("./coverage-reports");
        await Assert.That(options.CoverageFormat).IsEqualTo("xml");
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

    #region Line Number Filtering

    [Test]
    public async Task Parse_PathWithSingleLineNumber_ParsesLineFilter()
    {
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:15"]);

        await Assert.That(options.Path).IsEqualTo("test.spec.csx");
        await Assert.That(options.LineFilters).IsNotNull();
        await Assert.That(options.LineFilters!.Count).IsEqualTo(1);
        await Assert.That(options.LineFilters[0].File).IsEqualTo("test.spec.csx");
        await Assert.That(options.LineFilters[0].Lines).IsEquivalentTo(new[] { 15 });
    }

    [Test]
    public async Task Parse_PathWithMultipleLineNumbers_ParsesAllLines()
    {
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:15,20,25"]);

        await Assert.That(options.Path).IsEqualTo("test.spec.csx");
        await Assert.That(options.LineFilters).IsNotNull();
        await Assert.That(options.LineFilters!.Count).IsEqualTo(1);
        await Assert.That(options.LineFilters[0].Lines).IsEquivalentTo(new[] { 15, 20, 25 });
    }

    [Test]
    public async Task Parse_PathWithoutLineNumber_NoLineFilters()
    {
        var options = CliOptionsParser.Parse(["run", "test.spec.csx"]);

        await Assert.That(options.Path).IsEqualTo("test.spec.csx");
        await Assert.That(options.LineFilters).IsNull();
    }

    [Test]
    public async Task Parse_WindowsPath_NotTreatedAsLineNumber()
    {
        // Windows drive letter C: should not be confused with line number syntax
        var options = CliOptionsParser.Parse(["run", "C:\\path\\test.spec.csx"]);

        await Assert.That(options.Path).IsEqualTo("C:\\path\\test.spec.csx");
        await Assert.That(options.LineFilters).IsNull();
    }

    [Test]
    public async Task Parse_WindowsPathWithLineNumber_ParsesCorrectly()
    {
        // Windows path with line number should work
        var options = CliOptionsParser.Parse(["run", "C:\\path\\test.spec.csx:42"]);

        await Assert.That(options.Path).IsEqualTo("C:\\path\\test.spec.csx");
        await Assert.That(options.LineFilters).IsNotNull();
        await Assert.That(options.LineFilters![0].Lines).IsEquivalentTo(new[] { 42 });
    }

    [Test]
    public async Task Parse_PathWithColonButNoDigits_NotTreatedAsLineNumber()
    {
        // Colon followed by non-digits should not be parsed as line number
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:abc"]);

        await Assert.That(options.Path).IsEqualTo("test.spec.csx:abc");
        await Assert.That(options.LineFilters).IsNull();
    }

    [Test]
    public async Task Parse_RelativePathWithLineNumber_ParsesCorrectly()
    {
        var options = CliOptionsParser.Parse(["run", "./specs/test.spec.csx:10"]);

        await Assert.That(options.Path).IsEqualTo("./specs/test.spec.csx");
        await Assert.That(options.LineFilters).IsNotNull();
        await Assert.That(options.LineFilters![0].File).IsEqualTo("./specs/test.spec.csx");
        await Assert.That(options.LineFilters[0].Lines).IsEquivalentTo(new[] { 10 });
    }

    [Test]
    public async Task Parse_LineNumberZero_Ignored()
    {
        // Line number 0 should be filtered out
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:0,15"]);

        await Assert.That(options.LineFilters).IsNotNull();
        await Assert.That(options.LineFilters![0].Lines).IsEquivalentTo(new[] { 15 });
    }

    [Test]
    public async Task Parse_EmptyLineNumber_Ignored()
    {
        // Empty entries from consecutive commas should be ignored
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:15,,20"]);

        await Assert.That(options.LineFilters).IsNotNull();
        await Assert.That(options.LineFilters![0].Lines).IsEquivalentTo(new[] { 15, 20 });
    }

    #endregion

    #region List Command Options

    [Test]
    public async Task Parse_ListCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["list", "."]);

        await Assert.That(options.Command).IsEqualTo("list");
        await Assert.That(options.Path).IsEqualTo(".");
    }

    [Test]
    public async Task Parse_ListFormatOption_SetsListFormat()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--list-format", "json"]);

        await Assert.That(options.ListFormat).IsEqualTo("json");
    }

    [Test]
    public async Task Parse_ListFormatDefaultIsTree()
    {
        var options = CliOptionsParser.Parse(["list", "."]);

        await Assert.That(options.ListFormat).IsEqualTo("tree");
    }

    [Test]
    public async Task Parse_ListFormatIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--list-format", "JSON"]);

        await Assert.That(options.ListFormat).IsEqualTo("json");
    }

    [Test]
    public async Task Parse_ListFormatWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--list-format"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--list-format requires a value");
    }

    [Test]
    public async Task Parse_ShowLineNumbers_SetsShowLineNumbers()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--show-line-numbers"]);

        await Assert.That(options.ShowLineNumbers).IsTrue();
    }

    [Test]
    public async Task Parse_NoLineNumbers_ClearsShowLineNumbers()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--no-line-numbers"]);

        await Assert.That(options.ShowLineNumbers).IsFalse();
    }

    [Test]
    public async Task Parse_ShowLineNumbersDefaultIsTrue()
    {
        var options = CliOptionsParser.Parse(["list", "."]);

        await Assert.That(options.ShowLineNumbers).IsTrue();
    }

    [Test]
    public async Task Parse_FocusedOnlyFlag_SetsFocusedOnly()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--focused-only"]);

        await Assert.That(options.FocusedOnly).IsTrue();
    }

    [Test]
    public async Task Parse_PendingOnlyFlag_SetsPendingOnly()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--pending-only"]);

        await Assert.That(options.PendingOnly).IsTrue();
    }

    [Test]
    public async Task Parse_SkippedOnlyFlag_SetsSkippedOnly()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--skipped-only"]);

        await Assert.That(options.SkippedOnly).IsTrue();
    }

    [Test]
    public async Task Parse_StatusFiltersDefaultAreFalse()
    {
        var options = CliOptionsParser.Parse(["list", "."]);

        await Assert.That(options.FocusedOnly).IsFalse();
        await Assert.That(options.PendingOnly).IsFalse();
        await Assert.That(options.SkippedOnly).IsFalse();
    }

    [Test]
    public async Task Parse_MultipleStatusFilters_SetsAll()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--focused-only", "--pending-only"]);

        await Assert.That(options.FocusedOnly).IsTrue();
        await Assert.That(options.PendingOnly).IsTrue();
    }

    [Test]
    public async Task Parse_ListWithFilterName_SetsFilterName()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--filter-name", "Calculator"]);

        await Assert.That(options.FilterName).IsEqualTo("Calculator");
    }

    [Test]
    public async Task Parse_ListWithOutput_SetsOutputFile()
    {
        var options = CliOptionsParser.Parse(["list", ".", "-o", "specs.json"]);

        await Assert.That(options.OutputFile).IsEqualTo("specs.json");
    }

    [Test]
    public async Task Parse_ListAllOptions_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "list", "./specs",
            "--list-format", "flat",
            "--no-line-numbers",
            "--focused-only",
            "-o", "output.txt",
            "--filter-name", "User"
        ]);

        await Assert.That(options.Command).IsEqualTo("list");
        await Assert.That(options.Path).IsEqualTo("./specs");
        await Assert.That(options.ListFormat).IsEqualTo("flat");
        await Assert.That(options.ShowLineNumbers).IsFalse();
        await Assert.That(options.FocusedOnly).IsTrue();
        await Assert.That(options.OutputFile).IsEqualTo("output.txt");
        await Assert.That(options.FilterName).IsEqualTo("User");
    }

    #endregion
}

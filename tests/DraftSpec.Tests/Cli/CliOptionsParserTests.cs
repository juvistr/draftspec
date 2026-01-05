using DraftSpec.Cli;
using DraftSpec.Cli.Options.Enums;

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

        await Assert.That(options.Format).IsEqualTo(OutputFormat.Json);
    }

    [Test]
    public async Task Parse_ShortFormatOption_SetsFormat()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-f", "markdown"]);

        await Assert.That(options.Format).IsEqualTo(OutputFormat.Markdown);
    }

    [Test]
    public async Task Parse_FormatIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-f", "JSON"]);

        await Assert.That(options.Format).IsEqualTo(OutputFormat.Json);
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

        await Assert.That(options.Filter.FilterTags).IsEqualTo("fast,unit");
    }

    [Test]
    public async Task Parse_ShortFilterTagsOption_SetsFilterTags()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-t", "integration"]);

        await Assert.That(options.Filter.FilterTags).IsEqualTo("integration");
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

        await Assert.That(options.Filter.ExcludeTags).IsEqualTo("slow,flaky");
    }

    [Test]
    public async Task Parse_ShortExcludeTagsOption_SetsExcludeTags()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-x", "wip"]);

        await Assert.That(options.Filter.ExcludeTags).IsEqualTo("wip");
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

        await Assert.That(options.Filter.FilterName).IsEqualTo("Calculator.*add");
    }

    [Test]
    public async Task Parse_ShortFilterNameOption_SetsFilterName()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-n", "should.*return"]);

        await Assert.That(options.Filter.FilterName).IsEqualTo("should.*return");
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

        await Assert.That(options.Filter.ExcludeName).IsEqualTo(".*slow.*");
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

        await Assert.That(options.Filter.FilterTags).IsNull();
        await Assert.That(options.Filter.ExcludeTags).IsNull();
        await Assert.That(options.Filter.FilterName).IsNull();
        await Assert.That(options.Filter.ExcludeName).IsNull();
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

        await Assert.That(options.Filter.FilterTags).IsEqualTo("fast,unit");
        await Assert.That(options.Filter.ExcludeTags).IsEqualTo("slow");
        await Assert.That(options.Filter.FilterName).IsEqualTo("Calculator");
        await Assert.That(options.Filter.ExcludeName).IsEqualTo(".*deprecated.*");
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

        await Assert.That(options.Coverage.Enabled).IsTrue();
    }

    [Test]
    public async Task Parse_CoverageDefaultIsFalse()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.Coverage.Enabled).IsFalse();
    }

    [Test]
    public async Task Parse_CoverageOutputOption_SetsCoverageOutput()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage", "--coverage-output", "./reports"]);

        await Assert.That(options.Coverage.Output).IsEqualTo("./reports");
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

        await Assert.That(options.Coverage.Format).IsEqualTo(CoverageFormat.Xml);
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

        await Assert.That(options.Coverage.Format).IsEqualTo(CoverageFormat.Cobertura);
    }

    [Test]
    public async Task Parse_CoverageFormatIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--coverage-format", "COBERTURA"]);

        await Assert.That(options.Coverage.Format).IsEqualTo(CoverageFormat.Cobertura);
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

        await Assert.That(options.Coverage.Enabled).IsTrue();
        await Assert.That(options.Coverage.Output).IsEqualTo("./coverage-reports");
        await Assert.That(options.Coverage.Format).IsEqualTo(CoverageFormat.Xml);
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
        await Assert.That(options.Format).IsEqualTo(OutputFormat.Html);
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
        Assert.NotNull(options.Filter.LineFilters);
        await Assert.That(options.Filter.LineFilters.Count).IsEqualTo(1);
        await Assert.That(options.Filter.LineFilters[0].File).IsEqualTo("test.spec.csx");
        await Assert.That(options.Filter.LineFilters[0].Lines).IsEquivalentTo(new[] { 15 });
    }

    [Test]
    public async Task Parse_PathWithMultipleLineNumbers_ParsesAllLines()
    {
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:15,20,25"]);

        await Assert.That(options.Path).IsEqualTo("test.spec.csx");
        Assert.NotNull(options.Filter.LineFilters);
        await Assert.That(options.Filter.LineFilters.Count).IsEqualTo(1);
        await Assert.That(options.Filter.LineFilters[0].Lines).IsEquivalentTo(new[] { 15, 20, 25 });
    }

    [Test]
    public async Task Parse_PathWithoutLineNumber_NoLineFilters()
    {
        var options = CliOptionsParser.Parse(["run", "test.spec.csx"]);

        await Assert.That(options.Path).IsEqualTo("test.spec.csx");
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task Parse_WindowsPath_NotTreatedAsLineNumber()
    {
        // Windows drive letter C: should not be confused with line number syntax
        var options = CliOptionsParser.Parse(["run", "C:\\path\\test.spec.csx"]);

        await Assert.That(options.Path).IsEqualTo("C:\\path\\test.spec.csx");
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task Parse_WindowsPathWithLineNumber_ParsesCorrectly()
    {
        // Windows path with line number should work
        var options = CliOptionsParser.Parse(["run", "C:\\path\\test.spec.csx:42"]);

        await Assert.That(options.Path).IsEqualTo("C:\\path\\test.spec.csx");
        Assert.NotNull(options.Filter.LineFilters);
        await Assert.That(options.Filter.LineFilters[0].Lines).IsEquivalentTo(new[] { 42 });
    }

    [Test]
    public async Task Parse_PathWithColonButNoDigits_NotTreatedAsLineNumber()
    {
        // Colon followed by non-digits should not be parsed as line number
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:abc"]);

        await Assert.That(options.Path).IsEqualTo("test.spec.csx:abc");
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task Parse_RelativePathWithLineNumber_ParsesCorrectly()
    {
        var options = CliOptionsParser.Parse(["run", "./specs/test.spec.csx:10"]);

        await Assert.That(options.Path).IsEqualTo("./specs/test.spec.csx");
        Assert.NotNull(options.Filter.LineFilters);
        await Assert.That(options.Filter.LineFilters[0].File).IsEqualTo("./specs/test.spec.csx");
        await Assert.That(options.Filter.LineFilters[0].Lines).IsEquivalentTo(new[] { 10 });
    }

    [Test]
    public async Task Parse_LineNumberZero_Ignored()
    {
        // Line number 0 should be filtered out
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:0,15"]);

        Assert.NotNull(options.Filter.LineFilters);
        await Assert.That(options.Filter.LineFilters[0].Lines).IsEquivalentTo(new[] { 15 });
    }

    [Test]
    public async Task Parse_EmptyLineNumber_Ignored()
    {
        // Empty entries from consecutive commas should be ignored
        var options = CliOptionsParser.Parse(["run", "test.spec.csx:15,,20"]);

        Assert.NotNull(options.Filter.LineFilters);
        await Assert.That(options.Filter.LineFilters[0].Lines).IsEquivalentTo(new[] { 15, 20 });
    }

    #endregion

    #region Context Filtering

    [Test]
    public async Task Parse_ContextOption_SetsFilterContext()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--context", "UserService/CreateAsync"]);

        Assert.NotNull(options.Filter.FilterContext);
        await Assert.That(options.Filter.FilterContext.Count).IsEqualTo(1);
        await Assert.That(options.Filter.FilterContext[0]).IsEqualTo("UserService/CreateAsync");
    }

    [Test]
    public async Task Parse_ShortContextOption_SetsFilterContext()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-c", "UserService"]);

        Assert.NotNull(options.Filter.FilterContext);
        await Assert.That(options.Filter.FilterContext[0]).IsEqualTo("UserService");
    }

    [Test]
    public async Task Parse_MultipleContextOptions_AccumulatesAll()
    {
        var options = CliOptionsParser.Parse([
            "run", ".",
            "--context", "UserService/*",
            "--context", "OrderService/*"
        ]);

        Assert.NotNull(options.Filter.FilterContext);
        await Assert.That(options.Filter.FilterContext.Count).IsEqualTo(2);
        await Assert.That(options.Filter.FilterContext).Contains("UserService/*");
        await Assert.That(options.Filter.FilterContext).Contains("OrderService/*");
    }

    [Test]
    public async Task Parse_ContextWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--context"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--context requires a value");
    }

    [Test]
    public async Task Parse_ExcludeContextOption_SetsExcludeContext()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--exclude-context", "Legacy/*"]);

        Assert.NotNull(options.Filter.ExcludeContext);
        await Assert.That(options.Filter.ExcludeContext.Count).IsEqualTo(1);
        await Assert.That(options.Filter.ExcludeContext[0]).IsEqualTo("Legacy/*");
    }

    [Test]
    public async Task Parse_MultipleExcludeContextOptions_AccumulatesAll()
    {
        var options = CliOptionsParser.Parse([
            "run", ".",
            "--exclude-context", "Legacy/*",
            "--exclude-context", "**/Slow"
        ]);

        Assert.NotNull(options.Filter.ExcludeContext);
        await Assert.That(options.Filter.ExcludeContext.Count).IsEqualTo(2);
        await Assert.That(options.Filter.ExcludeContext).Contains("Legacy/*");
        await Assert.That(options.Filter.ExcludeContext).Contains("**/Slow");
    }

    [Test]
    public async Task Parse_ExcludeContextWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--exclude-context"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--exclude-context requires a value");
    }

    [Test]
    public async Task Parse_ContextFiltersDefaultAreNull()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        Assert.Null(options.Filter.FilterContext);
        Assert.Null(options.Filter.ExcludeContext);
    }

    [Test]
    public async Task Parse_CombinedContextAndExcludeContext_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "run", ".",
            "--context", "UserService/**",
            "--exclude-context", "**/Integration"
        ]);

        Assert.NotNull(options.Filter.FilterContext);
        await Assert.That(options.Filter.FilterContext[0]).IsEqualTo("UserService/**");
        Assert.NotNull(options.Filter.ExcludeContext);
        await Assert.That(options.Filter.ExcludeContext[0]).IsEqualTo("**/Integration");
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

        await Assert.That(options.ListFormat).IsEqualTo(ListFormat.Json);
    }

    [Test]
    public async Task Parse_ListFormatDefaultIsTree()
    {
        var options = CliOptionsParser.Parse(["list", "."]);

        await Assert.That(options.ListFormat).IsEqualTo(ListFormat.Tree);
    }

    [Test]
    public async Task Parse_ListFormatIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["list", ".", "--list-format", "JSON"]);

        await Assert.That(options.ListFormat).IsEqualTo(ListFormat.Json);
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

        await Assert.That(options.Filter.FilterName).IsEqualTo("Calculator");
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
        await Assert.That(options.ListFormat).IsEqualTo(ListFormat.Flat);
        await Assert.That(options.ShowLineNumbers).IsFalse();
        await Assert.That(options.FocusedOnly).IsTrue();
        await Assert.That(options.OutputFile).IsEqualTo("output.txt");
        await Assert.That(options.Filter.FilterName).IsEqualTo("User");
    }

    #endregion

    #region Stats Options

    [Test]
    public async Task Parse_NoStatsOption_SetsNoStats()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--no-stats"]);

        await Assert.That(options.NoStats).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.NoStats));
    }

    [Test]
    public async Task Parse_StatsOnlyOption_SetsStatsOnly()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--stats-only"]);

        await Assert.That(options.StatsOnly).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.StatsOnly));
    }

    [Test]
    public async Task Parse_DefaultNoStatsIsFalse()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.NoStats).IsFalse();
        await Assert.That(options.StatsOnly).IsFalse();
    }

    #endregion

    #region Partition Options

    [Test]
    public async Task Parse_PartitionOptions_SetsValues()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "4", "--partition-index", "2"]);

        await Assert.That(options.Partition.Total).IsEqualTo(4);
        await Assert.That(options.Partition.Index).IsEqualTo(2);
        await Assert.That(options.Partition.Strategy).IsEqualTo(PartitionStrategy.File);
        await Assert.That(options.Error).IsNull();
    }

    [Test]
    public async Task Parse_PartitionWithStrategy_SetsValues()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "4", "--partition-index", "0", "--partition-strategy", "spec-count"]);

        await Assert.That(options.Partition.Total).IsEqualTo(4);
        await Assert.That(options.Partition.Index).IsEqualTo(0);
        await Assert.That(options.Partition.Strategy).IsEqualTo(PartitionStrategy.SpecCount);
        await Assert.That(options.Error).IsNull();
    }

    [Test]
    public async Task Parse_PartitionWithoutIndex_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "4"]);

        await Assert.That(options.Error).IsEqualTo("--partition requires --partition-index");
    }

    [Test]
    public async Task Parse_PartitionIndexWithoutPartition_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition-index", "0"]);

        await Assert.That(options.Error).IsEqualTo("--partition-index requires --partition");
    }

    [Test]
    public async Task Parse_PartitionIndexTooLarge_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "4", "--partition-index", "4"]);

        await Assert.That(options.Error).IsEqualTo("--partition-index must be less than --partition (4)");
    }

    [Test]
    public async Task Parse_PartitionIndexEqualToPartition_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "4", "--partition-index", "5"]);

        await Assert.That(options.Error).IsEqualTo("--partition-index must be less than --partition (4)");
    }

    [Test]
    public async Task Parse_PartitionInvalid_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "abc"]);

        await Assert.That(options.Error).IsEqualTo("--partition must be a positive integer");
    }

    [Test]
    public async Task Parse_PartitionZero_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "0"]);

        await Assert.That(options.Error).IsEqualTo("--partition must be a positive integer");
    }

    [Test]
    public async Task Parse_PartitionNegative_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "-1"]);

        await Assert.That(options.Error).IsEqualTo("--partition must be a positive integer");
    }

    [Test]
    public async Task Parse_PartitionIndexNegative_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition-index", "-1"]);

        await Assert.That(options.Error).IsEqualTo("--partition-index must be a non-negative integer");
    }

    [Test]
    public async Task Parse_PartitionStrategyInvalid_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition-strategy", "invalid"]);

        await Assert.That(options.Error).IsEqualTo("Unknown partition strategy: 'invalid'. Valid options: file, spec-count");
    }

    [Test]
    public async Task Parse_PartitionStrategyFileValid()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "2", "--partition-index", "0", "--partition-strategy", "file"]);

        await Assert.That(options.Partition.Strategy).IsEqualTo(PartitionStrategy.File);
        await Assert.That(options.Error).IsNull();
    }

    [Test]
    public async Task Parse_PartitionStrategyIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "2", "--partition-index", "0", "--partition-strategy", "SPEC-COUNT"]);

        await Assert.That(options.Partition.Strategy).IsEqualTo(PartitionStrategy.SpecCount);
        await Assert.That(options.Error).IsNull();
    }

    [Test]
    public async Task Parse_PartitionOptionsMarkedAsExplicitlySet()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--partition", "4", "--partition-index", "2", "--partition-strategy", "spec-count"]);

        await Assert.That(options.ExplicitlySet).Contains("Total");
        await Assert.That(options.ExplicitlySet).Contains("Index");
        await Assert.That(options.ExplicitlySet).Contains("Strategy");
    }

    [Test]
    public async Task Parse_DefaultPartitionValues()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.Partition.Total).IsNull();
        await Assert.That(options.Partition.Index).IsNull();
        await Assert.That(options.Partition.Strategy).IsEqualTo(PartitionStrategy.File);
    }

    #endregion

    #region Incremental Options

    [Test]
    public async Task Parse_IncrementalLongFlag_SetsTrue()
    {
        var options = CliOptionsParser.Parse(["watch", ".", "--incremental"]);

        await Assert.That(options.Incremental).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Incremental));
    }

    [Test]
    public async Task Parse_IncrementalShortFlag_SetsTrue()
    {
        var options = CliOptionsParser.Parse(["watch", ".", "-i"]);

        await Assert.That(options.Incremental).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Incremental));
    }

    [Test]
    public async Task Parse_DefaultIncremental_IsFalse()
    {
        var options = CliOptionsParser.Parse(["watch", "."]);

        await Assert.That(options.Incremental).IsFalse();
    }

    [Test]
    public async Task Parse_IncrementalWithOtherOptions_Works()
    {
        var options = CliOptionsParser.Parse(["watch", ".", "--incremental", "--parallel"]);

        await Assert.That(options.Incremental).IsTrue();
        await Assert.That(options.Parallel).IsTrue();
    }

    #endregion

    #region Validate Command Options

    [Test]
    public async Task Parse_StaticFlag_SetsStatic()
    {
        var options = CliOptionsParser.Parse(["validate", ".", "--static"]);

        await Assert.That(options.Static).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Static));
    }

    [Test]
    public async Task Parse_StrictFlag_SetsStrict()
    {
        var options = CliOptionsParser.Parse(["validate", ".", "--strict"]);

        await Assert.That(options.Strict).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Strict));
    }

    [Test]
    public async Task Parse_QuietFlag_SetsQuiet()
    {
        var options = CliOptionsParser.Parse(["validate", ".", "--quiet"]);

        await Assert.That(options.Quiet).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Quiet));
    }

    [Test]
    public async Task Parse_ShortQuietFlag_SetsQuiet()
    {
        var options = CliOptionsParser.Parse(["validate", ".", "-q"]);

        await Assert.That(options.Quiet).IsTrue();
    }

    [Test]
    public async Task Parse_FilesFlag_SetsFiles()
    {
        var options = CliOptionsParser.Parse(["validate", ".", "--files", "a.spec.csx,b.spec.csx"]);

        Assert.NotNull(options.Files);
        await Assert.That(options.Files.Count).IsEqualTo(2);
        await Assert.That(options.Files).Contains("a.spec.csx");
        await Assert.That(options.Files).Contains("b.spec.csx");
    }

    [Test]
    public async Task Parse_FilesFlagWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["validate", ".", "--files"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--files requires a value");
    }

    [Test]
    public async Task Parse_ValidateWithAllOptions_Works()
    {
        var options = CliOptionsParser.Parse(["validate", ".", "--static", "--strict", "--quiet"]);

        await Assert.That(options.Command).IsEqualTo("validate");
        await Assert.That(options.Static).IsTrue();
        await Assert.That(options.Strict).IsTrue();
        await Assert.That(options.Quiet).IsTrue();
    }

    #endregion

    #region Test Impact Analysis Options

    [Test]
    public async Task Parse_AffectedByOption_SetsAffectedBy()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--affected-by", "HEAD~1"]);

        await Assert.That(options.AffectedBy).IsEqualTo("HEAD~1");
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.AffectedBy));
    }

    [Test]
    public async Task Parse_AffectedByStaged_SetsAffectedBy()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--affected-by", "staged"]);

        await Assert.That(options.AffectedBy).IsEqualTo("staged");
    }

    [Test]
    public async Task Parse_AffectedByBranch_SetsAffectedBy()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--affected-by", "main"]);

        await Assert.That(options.AffectedBy).IsEqualTo("main");
    }

    [Test]
    public async Task Parse_AffectedByWithFilePath_SetsAffectedBy()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--affected-by", "/tmp/changed-files.txt"]);

        await Assert.That(options.AffectedBy).IsEqualTo("/tmp/changed-files.txt");
    }

    [Test]
    public async Task Parse_AffectedByWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--affected-by"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--affected-by requires a value");
    }

    [Test]
    public async Task Parse_DryRunOption_SetsDryRun()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--dry-run"]);

        await Assert.That(options.DryRun).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.DryRun));
    }

    [Test]
    public async Task Parse_AffectedByWithDryRun_SetsAll()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--affected-by", "HEAD~1", "--dry-run"]);

        await Assert.That(options.AffectedBy).IsEqualTo("HEAD~1");
        await Assert.That(options.DryRun).IsTrue();
    }

    [Test]
    public async Task Parse_DefaultAffectedByIsNull()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.AffectedBy).IsNull();
    }

    [Test]
    public async Task Parse_DefaultDryRunIsFalse()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.DryRun).IsFalse();
    }

    #endregion

    #region Flaky Command Options

    [Test]
    public async Task Parse_FlakyCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["flaky", "."]);

        await Assert.That(options.Command).IsEqualTo("flaky");
        await Assert.That(options.Path).IsEqualTo(".");
    }

    [Test]
    public async Task Parse_QuarantineFlag_SetsQuarantine()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--quarantine"]);

        await Assert.That(options.Quarantine).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Quarantine));
    }

    [Test]
    public async Task Parse_QuarantineDefaultIsFalse()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.Quarantine).IsFalse();
    }

    [Test]
    public async Task Parse_NoHistoryFlag_SetsNoHistory()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--no-history"]);

        await Assert.That(options.NoHistory).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.NoHistory));
    }

    [Test]
    public async Task Parse_NoHistoryDefaultIsFalse()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.NoHistory).IsFalse();
    }

    [Test]
    public async Task Parse_MinChangesOption_SetsMinStatusChanges()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--min-changes", "3"]);

        await Assert.That(options.MinStatusChanges).IsEqualTo(3);
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.MinStatusChanges));
    }

    [Test]
    public async Task Parse_MinChangesWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--min-changes"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--min-changes requires a value");
    }

    [Test]
    public async Task Parse_MinChangesInvalidValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--min-changes", "abc"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--min-changes must be a positive integer");
    }

    [Test]
    public async Task Parse_MinChangesZero_SetsError()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--min-changes", "0"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--min-changes must be a positive integer");
    }

    [Test]
    public async Task Parse_MinChangesDefaultIs2()
    {
        var options = CliOptionsParser.Parse(["flaky", "."]);

        await Assert.That(options.MinStatusChanges).IsEqualTo(2);
    }

    [Test]
    public async Task Parse_WindowSizeOption_SetsWindowSize()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--window-size", "20"]);

        await Assert.That(options.WindowSize).IsEqualTo(20);
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.WindowSize));
    }

    [Test]
    public async Task Parse_WindowSizeWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--window-size"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--window-size requires a value");
    }

    [Test]
    public async Task Parse_WindowSizeInvalidValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--window-size", "abc"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--window-size must be at least 2");
    }

    [Test]
    public async Task Parse_WindowSizeOne_SetsError()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--window-size", "1"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--window-size must be at least 2");
    }

    [Test]
    public async Task Parse_WindowSizeDefaultIs10()
    {
        var options = CliOptionsParser.Parse(["flaky", "."]);

        await Assert.That(options.WindowSize).IsEqualTo(10);
    }

    [Test]
    public async Task Parse_ClearOption_SetsClear()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--clear", "test.spec.csx:Context/spec1"]);

        await Assert.That(options.Clear).IsEqualTo("test.spec.csx:Context/spec1");
    }

    [Test]
    public async Task Parse_ClearWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["flaky", ".", "--clear"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--clear requires a spec ID");
    }

    [Test]
    public async Task Parse_FlakyWithAllOptions_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "flaky", "./specs",
            "--min-changes", "3",
            "--window-size", "15"
        ]);

        await Assert.That(options.Command).IsEqualTo("flaky");
        await Assert.That(options.Path).IsEqualTo("./specs");
        await Assert.That(options.MinStatusChanges).IsEqualTo(3);
        await Assert.That(options.WindowSize).IsEqualTo(15);
    }

    [Test]
    public async Task Parse_RunWithQuarantineAndNoHistory_SetsAll()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--quarantine", "--no-history"]);

        await Assert.That(options.Command).IsEqualTo("run");
        await Assert.That(options.Quarantine).IsTrue();
        await Assert.That(options.NoHistory).IsTrue();
    }

    #endregion

    #region Estimate Command Options

    [Test]
    public async Task Parse_EstimateCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["estimate", "."]);

        await Assert.That(options.Command).IsEqualTo("estimate");
        await Assert.That(options.Path).IsEqualTo(".");
    }

    [Test]
    public async Task Parse_EstimateCommandWithoutPath_UsesDefaultPath()
    {
        var options = CliOptionsParser.Parse(["estimate"]);

        await Assert.That(options.Command).IsEqualTo("estimate");
        await Assert.That(options.Path).IsEqualTo(".");
    }

    [Test]
    public async Task Parse_PercentileOption_SetsPercentile()
    {
        var options = CliOptionsParser.Parse(["estimate", ".", "--percentile", "95"]);

        await Assert.That(options.Percentile).IsEqualTo(95);
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Percentile));
    }

    [Test]
    public async Task Parse_PercentileWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["estimate", ".", "--percentile"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--percentile requires a value");
    }

    [Test]
    public async Task Parse_PercentileInvalidValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["estimate", ".", "--percentile", "abc"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--percentile must be between 1 and 99");
    }

    [Test]
    public async Task Parse_PercentileZero_SetsError()
    {
        var options = CliOptionsParser.Parse(["estimate", ".", "--percentile", "0"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--percentile must be between 1 and 99");
    }

    [Test]
    public async Task Parse_PercentileHundred_SetsError()
    {
        var options = CliOptionsParser.Parse(["estimate", ".", "--percentile", "100"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--percentile must be between 1 and 99");
    }

    [Test]
    public async Task Parse_PercentileNegative_SetsError()
    {
        var options = CliOptionsParser.Parse(["estimate", ".", "--percentile", "-5"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--percentile must be between 1 and 99");
    }

    [Test]
    public async Task Parse_PercentileDefaultIs50()
    {
        var options = CliOptionsParser.Parse(["estimate", "."]);

        await Assert.That(options.Percentile).IsEqualTo(50);
    }

    [Test]
    public async Task Parse_OutputSecondsFlag_SetsOutputSeconds()
    {
        var options = CliOptionsParser.Parse(["estimate", ".", "--output-seconds"]);

        await Assert.That(options.OutputSeconds).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.OutputSeconds));
    }

    [Test]
    public async Task Parse_OutputSecondsDefaultIsFalse()
    {
        var options = CliOptionsParser.Parse(["estimate", "."]);

        await Assert.That(options.OutputSeconds).IsFalse();
    }

    [Test]
    public async Task Parse_EstimateWithAllOptions_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "estimate", "./specs",
            "--percentile", "95",
            "--output-seconds"
        ]);

        await Assert.That(options.Command).IsEqualTo("estimate");
        await Assert.That(options.Path).IsEqualTo("./specs");
        await Assert.That(options.Percentile).IsEqualTo(95);
        await Assert.That(options.OutputSeconds).IsTrue();
    }

    #endregion

    #region Cache Command Options

    [Test]
    public async Task Parse_CacheCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["cache", "stats"]);

        await Assert.That(options.Command).IsEqualTo("cache");
        await Assert.That(options.CacheSubcommand).IsEqualTo("stats");
    }

    [Test]
    public async Task Parse_CacheStatsWithPath_SetsBoth()
    {
        var options = CliOptionsParser.Parse(["cache", "stats", "./project"]);

        await Assert.That(options.Command).IsEqualTo("cache");
        await Assert.That(options.CacheSubcommand).IsEqualTo("stats");
        await Assert.That(options.Path).IsEqualTo("./project");
    }

    [Test]
    public async Task Parse_CacheClearCommand_SetsClear()
    {
        var options = CliOptionsParser.Parse(["cache", "clear"]);

        await Assert.That(options.Command).IsEqualTo("cache");
        await Assert.That(options.CacheSubcommand).IsEqualTo("clear");
    }

    [Test]
    public async Task Parse_CacheClearWithPath_SetsBoth()
    {
        var options = CliOptionsParser.Parse(["cache", "clear", "./my-project"]);

        await Assert.That(options.Command).IsEqualTo("cache");
        await Assert.That(options.CacheSubcommand).IsEqualTo("clear");
        await Assert.That(options.Path).IsEqualTo("./my-project");
    }

    [Test]
    public async Task Parse_CacheSubcommandIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["cache", "CLEAR"]);

        await Assert.That(options.CacheSubcommand).IsEqualTo("clear");
    }

    [Test]
    public async Task Parse_CacheWithoutSubcommand_UsesDefaultStats()
    {
        var options = CliOptionsParser.Parse(["cache"]);

        await Assert.That(options.Command).IsEqualTo("cache");
        await Assert.That(options.CacheSubcommand).IsEqualTo("stats");
    }

    [Test]
    public async Task Parse_CacheDefaultPath_IsDot()
    {
        var options = CliOptionsParser.Parse(["cache", "stats"]);

        await Assert.That(options.Path).IsEqualTo(".");
    }

    #endregion

    #region Docs Command Options

    [Test]
    public async Task Parse_DocsCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["docs", "."]);

        await Assert.That(options.Command).IsEqualTo("docs");
        await Assert.That(options.Path).IsEqualTo(".");
    }

    [Test]
    public async Task Parse_DocsCommandWithoutPath_UsesDefaultPath()
    {
        var options = CliOptionsParser.Parse(["docs"]);

        await Assert.That(options.Command).IsEqualTo("docs");
        await Assert.That(options.Path).IsEqualTo(".");
    }

    [Test]
    public async Task Parse_DocsFormatMarkdown_SetsFormat()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--docs-format", "markdown"]);

        await Assert.That(options.DocsFormat).IsEqualTo(DocsFormat.Markdown);
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.DocsFormat));
    }

    [Test]
    public async Task Parse_DocsFormatMd_SetsFormat()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--docs-format", "md"]);

        await Assert.That(options.DocsFormat).IsEqualTo(DocsFormat.Markdown);
    }

    [Test]
    public async Task Parse_DocsFormatHtml_SetsFormat()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--docs-format", "html"]);

        await Assert.That(options.DocsFormat).IsEqualTo(DocsFormat.Html);
    }

    [Test]
    public async Task Parse_DocsFormatIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--docs-format", "HTML"]);

        await Assert.That(options.DocsFormat).IsEqualTo(DocsFormat.Html);
    }

    [Test]
    public async Task Parse_DocsFormatWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--docs-format"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--docs-format requires a value");
    }

    [Test]
    public async Task Parse_DocsFormatUnknownValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--docs-format", "pdf"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("Unknown docs format: 'pdf'");
        await Assert.That(options.Error).Contains("markdown, html");
    }

    [Test]
    public async Task Parse_DocsFormatDefaultIsMarkdown()
    {
        var options = CliOptionsParser.Parse(["docs", "."]);

        await Assert.That(options.DocsFormat).IsEqualTo(DocsFormat.Markdown);
    }

    [Test]
    public async Task Parse_DocsContext_SetsDocsContext()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--docs-context", "UserService"]);

        await Assert.That(options.DocsContext).IsEqualTo("UserService");
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.DocsContext));
    }

    [Test]
    public async Task Parse_DocsContextWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--docs-context"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--docs-context requires a value");
    }

    [Test]
    public async Task Parse_DocsContextDefaultIsNull()
    {
        var options = CliOptionsParser.Parse(["docs", "."]);

        await Assert.That(options.DocsContext).IsNull();
    }

    [Test]
    public async Task Parse_WithResultsFlag_SetsWithResults()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--with-results"]);

        await Assert.That(options.WithResults).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.WithResults));
    }

    [Test]
    public async Task Parse_WithResultsDefaultIsFalse()
    {
        var options = CliOptionsParser.Parse(["docs", "."]);

        await Assert.That(options.WithResults).IsFalse();
    }

    [Test]
    public async Task Parse_ResultsFile_SetsResultsFile()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--results-file", "results.json"]);

        await Assert.That(options.ResultsFile).IsEqualTo("results.json");
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.ResultsFile));
    }

    [Test]
    public async Task Parse_ResultsFileWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--results-file"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--results-file requires a path");
    }

    [Test]
    public async Task Parse_ResultsFileDefaultIsNull()
    {
        var options = CliOptionsParser.Parse(["docs", "."]);

        await Assert.That(options.ResultsFile).IsNull();
    }

    [Test]
    public async Task Parse_DocsWithAllOptions_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "docs", "./specs",
            "--docs-format", "html",
            "--docs-context", "UserService",
            "--with-results",
            "--results-file", "results.json"
        ]);

        await Assert.That(options.Command).IsEqualTo("docs");
        await Assert.That(options.Path).IsEqualTo("./specs");
        await Assert.That(options.DocsFormat).IsEqualTo(DocsFormat.Html);
        await Assert.That(options.DocsContext).IsEqualTo("UserService");
        await Assert.That(options.WithResults).IsTrue();
        await Assert.That(options.ResultsFile).IsEqualTo("results.json");
    }

    [Test]
    public async Task Parse_DocsWithFilterName_SetsFilterName()
    {
        var options = CliOptionsParser.Parse(["docs", ".", "--filter-name", "Calculator"]);

        await Assert.That(options.Filter.FilterName).IsEqualTo("Calculator");
    }

    #endregion

    #region Coverage-Map Command Options

    [Test]
    public async Task Parse_CoverageMapCommand_SetsCommand()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/"]);

        await Assert.That(options.Command).IsEqualTo("coverage-map");
        await Assert.That(options.CoverageMapSourcePath).IsEqualTo("src/");
    }

    [Test]
    public async Task Parse_CoverageMapWithoutPath_UsesDefaultPath()
    {
        var options = CliOptionsParser.Parse(["coverage-map"]);

        await Assert.That(options.Command).IsEqualTo("coverage-map");
        await Assert.That(options.CoverageMapSourcePath).IsNull();
    }

    [Test]
    public async Task Parse_CoverageMapGapsOnly_SetsGapsOnly()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--gaps-only"]);

        await Assert.That(options.GapsOnly).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.GapsOnly));
    }

    [Test]
    public async Task Parse_CoverageMapGapsOnlyDefault_IsFalse()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/"]);

        await Assert.That(options.GapsOnly).IsFalse();
    }

    [Test]
    public async Task Parse_CoverageMapSpecs_SetsSpecPath()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--specs", "specs/"]);

        await Assert.That(options.CoverageMapSpecPath).IsEqualTo("specs/");
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.CoverageMapSpecPath));
    }

    [Test]
    public async Task Parse_CoverageMapSpecsWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--specs"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--specs requires a path");
    }

    [Test]
    public async Task Parse_CoverageMapNamespace_SetsNamespaceFilter()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--namespace", "MyApp.Services"]);

        await Assert.That(options.CoverageMapNamespaceFilter).IsEqualTo("MyApp.Services");
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.CoverageMapNamespaceFilter));
    }

    [Test]
    public async Task Parse_CoverageMapNamespaceWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--namespace"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--namespace requires a value");
    }

    [Test]
    public async Task Parse_CoverageMapFormatJson_SetsFormat()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--coverage-map-format", "json"]);

        await Assert.That(options.CoverageMapFormat).IsEqualTo(CoverageMapFormat.Json);
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.CoverageMapFormat));
    }

    [Test]
    public async Task Parse_CoverageMapFormatConsole_SetsFormat()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--coverage-map-format", "console"]);

        await Assert.That(options.CoverageMapFormat).IsEqualTo(CoverageMapFormat.Console);
    }

    [Test]
    public async Task Parse_CoverageMapFormatIsCaseInsensitive()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--coverage-map-format", "JSON"]);

        await Assert.That(options.CoverageMapFormat).IsEqualTo(CoverageMapFormat.Json);
    }

    [Test]
    public async Task Parse_CoverageMapFormatWithoutValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--coverage-map-format"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("--coverage-map-format requires a value");
    }

    [Test]
    public async Task Parse_CoverageMapFormatUnknownValue_SetsError()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/", "--coverage-map-format", "xml"]);

        await Assert.That(options.Error).IsNotNull();
        await Assert.That(options.Error).Contains("Unknown coverage-map format: 'xml'");
        await Assert.That(options.Error).Contains("console, json");
    }

    [Test]
    public async Task Parse_CoverageMapFormatDefaultIsConsole()
    {
        var options = CliOptionsParser.Parse(["coverage-map", "src/"]);

        await Assert.That(options.CoverageMapFormat).IsEqualTo(CoverageMapFormat.Console);
    }

    [Test]
    public async Task Parse_CoverageMapAllOptions_SetsAll()
    {
        var options = CliOptionsParser.Parse([
            "coverage-map", "src/Services/",
            "--specs", "specs/",
            "--namespace", "MyApp.Services,MyApp.Handlers",
            "--coverage-map-format", "json",
            "--gaps-only"
        ]);

        await Assert.That(options.Command).IsEqualTo("coverage-map");
        await Assert.That(options.CoverageMapSourcePath).IsEqualTo("src/Services/");
        await Assert.That(options.CoverageMapSpecPath).IsEqualTo("specs/");
        await Assert.That(options.CoverageMapNamespaceFilter).IsEqualTo("MyApp.Services,MyApp.Handlers");
        await Assert.That(options.CoverageMapFormat).IsEqualTo(CoverageMapFormat.Json);
        await Assert.That(options.GapsOnly).IsTrue();
    }

    #endregion

    #region Interactive Flag

    [Test]
    public async Task Parse_InteractiveFlag_SetsInteractive()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--interactive"]);

        await Assert.That(options.Interactive).IsTrue();
    }

    [Test]
    public async Task Parse_InteractiveShortFlag_SetsInteractive()
    {
        var options = CliOptionsParser.Parse(["run", ".", "-I"]);

        await Assert.That(options.Interactive).IsTrue();
    }

    [Test]
    public async Task Parse_InteractiveFlag_AddsToExplicitlySet()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--interactive"]);

        await Assert.That(options.ExplicitlySet).Contains(nameof(options.Interactive));
    }

    [Test]
    public async Task Parse_WithoutInteractiveFlag_InteractiveIsFalse()
    {
        var options = CliOptionsParser.Parse(["run", "."]);

        await Assert.That(options.Interactive).IsFalse();
    }

    [Test]
    public async Task Parse_InteractiveWithOtherFlags_AllFlagsSet()
    {
        var options = CliOptionsParser.Parse(["run", ".", "--interactive", "--parallel", "--bail"]);

        await Assert.That(options.Interactive).IsTrue();
        await Assert.That(options.Parallel).IsTrue();
        await Assert.That(options.Bail).IsTrue();
    }

    #endregion
}

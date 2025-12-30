using DraftSpec.Cli;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for CliOptions conversion methods to command-specific options.
/// </summary>
public class CliOptionsConversionTests
{
    #region ToRunOptions Tests

    [Test]
    public async Task ToRunOptions_MapsBasicProperties()
    {
        var cliOptions = new CliOptions
        {
            Path = "/test/path",
            Format = OutputFormat.Json,
            OutputFile = "report.json",
            CssUrl = "custom.css",
            Parallel = true,
            NoCache = true,
            Bail = true,
            NoStats = true,
            StatsOnly = true,
            Reporters = "console,junit"
        };

        var runOptions = cliOptions.ToRunOptions();

        await Assert.That(runOptions.Path).IsEqualTo("/test/path");
        await Assert.That(runOptions.Format).IsEqualTo(OutputFormat.Json);
        await Assert.That(runOptions.OutputFile).IsEqualTo("report.json");
        await Assert.That(runOptions.CssUrl).IsEqualTo("custom.css");
        await Assert.That(runOptions.Parallel).IsTrue();
        await Assert.That(runOptions.NoCache).IsTrue();
        await Assert.That(runOptions.Bail).IsTrue();
        await Assert.That(runOptions.NoStats).IsTrue();
        await Assert.That(runOptions.StatsOnly).IsTrue();
        await Assert.That(runOptions.Reporters).IsEqualTo("console,junit");
    }

    [Test]
    public async Task ToRunOptions_MapsFilterOptions()
    {
        var cliOptions = new CliOptions
        {
            FilterTags = "unit",
            ExcludeTags = "slow",
            FilterName = "should pass",
            ExcludeName = "integration",
            FilterContext = ["Calculator"],
            ExcludeContext = ["Legacy"],
            LineFilters = [new LineFilter("test.spec.csx", [1, 2, 3])]
        };

        var runOptions = cliOptions.ToRunOptions();

        await Assert.That(runOptions.Filter.FilterTags).IsEqualTo("unit");
        await Assert.That(runOptions.Filter.ExcludeTags).IsEqualTo("slow");
        await Assert.That(runOptions.Filter.FilterName).IsEqualTo("should pass");
        await Assert.That(runOptions.Filter.ExcludeName).IsEqualTo("integration");
        await Assert.That(runOptions.Filter.FilterContext).IsEquivalentTo(["Calculator"]);
        await Assert.That(runOptions.Filter.ExcludeContext).IsEquivalentTo(["Legacy"]);
        await Assert.That(runOptions.Filter.LineFilters).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ToRunOptions_MapsCoverageOptions()
    {
        var cliOptions = new CliOptions
        {
            Coverage = true,
            CoverageOutput = "coverage.xml",
            CoverageFormat = CoverageFormat.Cobertura,
            CoverageReportFormats = "html,json"
        };

        var runOptions = cliOptions.ToRunOptions();

        await Assert.That(runOptions.Coverage.Enabled).IsTrue();
        await Assert.That(runOptions.Coverage.Output).IsEqualTo("coverage.xml");
        await Assert.That(runOptions.Coverage.Format).IsEqualTo(CoverageFormat.Cobertura);
        await Assert.That(runOptions.Coverage.ReportFormats).IsEqualTo("html,json");
    }

    [Test]
    public async Task ToRunOptions_MapsPartitionOptions()
    {
        var cliOptions = new CliOptions
        {
            Partition = 4,
            PartitionIndex = 2,
            PartitionStrategy = PartitionStrategy.SpecCount
        };

        var runOptions = cliOptions.ToRunOptions();

        await Assert.That(runOptions.Partition.Total).IsEqualTo(4);
        await Assert.That(runOptions.Partition.Index).IsEqualTo(2);
        await Assert.That(runOptions.Partition.Strategy).IsEqualTo(PartitionStrategy.SpecCount);
    }

    [Test]
    public async Task ToRunOptions_DefaultValues_CreatesValidOptions()
    {
        var cliOptions = new CliOptions();

        var runOptions = cliOptions.ToRunOptions();

        await Assert.That(runOptions).IsNotNull();
        await Assert.That(runOptions.Path).IsEqualTo(".");
        await Assert.That(runOptions.Filter).IsNotNull();
        await Assert.That(runOptions.Coverage).IsNotNull();
        await Assert.That(runOptions.Partition).IsNotNull();
    }

    #endregion

    #region ToListOptions Tests

    [Test]
    public async Task ToListOptions_MapsBasicProperties()
    {
        var cliOptions = new CliOptions
        {
            Path = "/specs",
            ListFormat = ListFormat.Json,
            ShowLineNumbers = true,
            FocusedOnly = true,
            PendingOnly = true,
            SkippedOnly = true
        };

        var listOptions = cliOptions.ToListOptions();

        await Assert.That(listOptions.Path).IsEqualTo("/specs");
        await Assert.That(listOptions.Format).IsEqualTo(ListFormat.Json);
        await Assert.That(listOptions.ShowLineNumbers).IsTrue();
        await Assert.That(listOptions.FocusedOnly).IsTrue();
        await Assert.That(listOptions.PendingOnly).IsTrue();
        await Assert.That(listOptions.SkippedOnly).IsTrue();
    }

    [Test]
    public async Task ToListOptions_MapsFilterOptions()
    {
        var cliOptions = new CliOptions
        {
            FilterTags = "smoke",
            FilterName = "calculator"
        };

        var listOptions = cliOptions.ToListOptions();

        await Assert.That(listOptions.Filter.FilterTags).IsEqualTo("smoke");
        await Assert.That(listOptions.Filter.FilterName).IsEqualTo("calculator");
    }

    [Test]
    public async Task ToListOptions_DefaultValues_CreatesValidOptions()
    {
        var cliOptions = new CliOptions();

        var listOptions = cliOptions.ToListOptions();

        await Assert.That(listOptions).IsNotNull();
        await Assert.That(listOptions.Path).IsEqualTo(".");
        await Assert.That(listOptions.Format).IsEqualTo(ListFormat.Tree);
        await Assert.That(listOptions.Filter).IsNotNull();
    }

    #endregion

    #region ToValidateOptions Tests

    [Test]
    public async Task ToValidateOptions_MapsAllProperties()
    {
        var cliOptions = new CliOptions
        {
            Path = "/validate/path",
            Static = true,
            Strict = true,
            Quiet = true,
            Files = ["file1.spec.csx", "file2.spec.csx"]
        };

        var validateOptions = cliOptions.ToValidateOptions();

        await Assert.That(validateOptions.Path).IsEqualTo("/validate/path");
        await Assert.That(validateOptions.Static).IsTrue();
        await Assert.That(validateOptions.Strict).IsTrue();
        await Assert.That(validateOptions.Quiet).IsTrue();
        await Assert.That(validateOptions.Files).IsEquivalentTo(["file1.spec.csx", "file2.spec.csx"]);
    }

    [Test]
    public async Task ToValidateOptions_DefaultValues_CreatesValidOptions()
    {
        var cliOptions = new CliOptions();

        var validateOptions = cliOptions.ToValidateOptions();

        await Assert.That(validateOptions).IsNotNull();
        await Assert.That(validateOptions.Path).IsEqualTo(".");
        await Assert.That(validateOptions.Static).IsFalse();
        await Assert.That(validateOptions.Strict).IsFalse();
        await Assert.That(validateOptions.Quiet).IsFalse();
    }

    #endregion

    #region ToWatchOptions Tests

    [Test]
    public async Task ToWatchOptions_MapsBasicProperties()
    {
        var cliOptions = new CliOptions
        {
            Path = "/watch/path",
            Format = OutputFormat.Console,
            Incremental = true,
            Parallel = true,
            NoCache = true,
            Bail = true
        };

        var watchOptions = cliOptions.ToWatchOptions();

        await Assert.That(watchOptions.Path).IsEqualTo("/watch/path");
        await Assert.That(watchOptions.Format).IsEqualTo(OutputFormat.Console);
        await Assert.That(watchOptions.Incremental).IsTrue();
        await Assert.That(watchOptions.Parallel).IsTrue();
        await Assert.That(watchOptions.NoCache).IsTrue();
        await Assert.That(watchOptions.Bail).IsTrue();
    }

    [Test]
    public async Task ToWatchOptions_MapsFilterOptions()
    {
        var cliOptions = new CliOptions
        {
            FilterTags = "unit",
            FilterName = "quick"
        };

        var watchOptions = cliOptions.ToWatchOptions();

        await Assert.That(watchOptions.Filter.FilterTags).IsEqualTo("unit");
        await Assert.That(watchOptions.Filter.FilterName).IsEqualTo("quick");
    }

    [Test]
    public async Task ToWatchOptions_DefaultValues_CreatesValidOptions()
    {
        var cliOptions = new CliOptions();

        var watchOptions = cliOptions.ToWatchOptions();

        await Assert.That(watchOptions).IsNotNull();
        await Assert.That(watchOptions.Path).IsEqualTo(".");
        await Assert.That(watchOptions.Filter).IsNotNull();
        await Assert.That(watchOptions.Incremental).IsFalse();
    }

    #endregion

    #region SpecName to Filter Mapping

    [Test]
    public async Task ToRunOptions_SpecName_MappedToFilterSpecName()
    {
        var cliOptions = new CliOptions
        {
            SpecName = "TodoService.spec.csx"
        };

        var runOptions = cliOptions.ToRunOptions();

        await Assert.That(runOptions.Filter.SpecName).IsEqualTo("TodoService.spec.csx");
    }

    [Test]
    public async Task ToListOptions_SpecName_MappedToFilterSpecName()
    {
        var cliOptions = new CliOptions
        {
            SpecName = "Calculator.spec.csx"
        };

        var listOptions = cliOptions.ToListOptions();

        await Assert.That(listOptions.Filter.SpecName).IsEqualTo("Calculator.spec.csx");
    }

    #endregion
}

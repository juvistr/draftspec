using DraftSpec.Cli;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Parsing;

namespace DraftSpec.Tests.Cli.Parsing;

/// <summary>
/// Tests for individual option handlers.
/// </summary>
public class OptionHandlersTests
{
    #region Help Handler

    [Test]
    public async Task HandleHelp_SetsShowHelp()
    {
        var options = new CliOptions();

        var result = OptionHandlers.HandleHelp([], 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(1);
        await Assert.That(result.Error).IsNull();
        await Assert.That(options.ShowHelp).IsTrue();
    }

    #endregion

    #region Format Handler

    [Test]
    public async Task HandleFormat_ValidFormat_SetsFormatAndConsumesTwoArgs()
    {
        var options = new CliOptions();
        var args = new[] { "--format", "json" };

        var result = OptionHandlers.HandleFormat(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(result.Error).IsNull();
        await Assert.That(options.Format).IsEqualTo(OutputFormat.Json);
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Format));
    }

    [Test]
    public async Task HandleFormat_MissingValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--format" };

        var result = OptionHandlers.HandleFormat(args, 0, options);

        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error).Contains("--format requires a value");
    }

    [Test]
    public async Task HandleFormat_InvalidFormat_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--format", "invalid" };

        var result = OptionHandlers.HandleFormat(args, 0, options);

        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error).Contains("Unknown format");
    }

    #endregion

    #region Output Handler

    [Test]
    public async Task HandleOutput_ValidPath_SetsOutputFile()
    {
        var options = new CliOptions();
        var args = new[] { "--output", "report.json" };

        var result = OptionHandlers.HandleOutput(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.OutputFile).IsEqualTo("report.json");
    }

    [Test]
    public async Task HandleOutput_MissingValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--output" };

        var result = OptionHandlers.HandleOutput(args, 0, options);

        await Assert.That(result.Error).Contains("--output requires a file path");
    }

    #endregion

    #region Flag Handlers

    [Test]
    public async Task HandleForce_SetsForceFlag()
    {
        var options = new CliOptions();

        var result = OptionHandlers.HandleForce([], 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(1);
        await Assert.That(options.Force).IsTrue();
    }

    [Test]
    public async Task HandleParallel_SetsParallelFlag()
    {
        var options = new CliOptions();

        var result = OptionHandlers.HandleParallel([], 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(1);
        await Assert.That(options.Parallel).IsTrue();
        await Assert.That(options.ExplicitlySet).Contains(nameof(CliOptions.Parallel));
    }

    [Test]
    public async Task HandleNoCache_SetsNoCacheFlag()
    {
        var options = new CliOptions();

        var result = OptionHandlers.HandleNoCache([], 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(1);
        await Assert.That(options.NoCache).IsTrue();
    }

    [Test]
    public async Task HandleBail_SetsBailFlag()
    {
        var options = new CliOptions();

        var result = OptionHandlers.HandleBail([], 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(1);
        await Assert.That(options.Bail).IsTrue();
    }

    #endregion

    #region Filter Handlers

    [Test]
    public async Task HandleFilterTags_ValidTags_SetsFilterTags()
    {
        var options = new CliOptions();
        var args = new[] { "--filter-tags", "smoke,unit" };

        var result = OptionHandlers.HandleFilterTags(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.Filter.FilterTags).IsEqualTo("smoke,unit");
    }

    [Test]
    public async Task HandleContext_AddsToList()
    {
        var options = new CliOptions();
        var args = new[] { "--context", "Feature/Login" };

        var result = OptionHandlers.HandleContext(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        var filterContext = options.Filter.FilterContext;
        await Assert.That(filterContext).IsNotNull();
        await Assert.That(filterContext!).Contains("Feature/Login");
    }

    [Test]
    public async Task HandleContext_MultipleValues_AccumulatesList()
    {
        var options = new CliOptions();

        OptionHandlers.HandleContext(["--context", "Context1"], 0, options);
        OptionHandlers.HandleContext(["--context", "Context2"], 0, options);

        var filterContext = options.Filter.FilterContext;
        await Assert.That(filterContext).IsNotNull();
        await Assert.That(filterContext!).Contains("Context1");
        await Assert.That(filterContext!).Contains("Context2");
    }

    #endregion

    #region Coverage Handlers

    [Test]
    public async Task HandleCoverageFormat_ValidFormat_SetsFormat()
    {
        var options = new CliOptions();
        var args = new[] { "--coverage-format", "cobertura" };

        var result = OptionHandlers.HandleCoverageFormat(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.Coverage.Format).IsEqualTo(CoverageFormat.Cobertura);
    }

    [Test]
    public async Task HandleCoverageFormat_InvalidFormat_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--coverage-format", "invalid" };

        var result = OptionHandlers.HandleCoverageFormat(args, 0, options);

        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error).Contains("Unknown coverage format");
    }

    [Test]
    public async Task HandleCoverageFormat_MissingValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--coverage-format" };

        var result = OptionHandlers.HandleCoverageFormat(args, 0, options);

        await Assert.That(result.Error).Contains("--coverage-format requires a value");
    }

    [Test]
    public async Task HandleCoverageReportFormats_ValidFormats_SetsFormats()
    {
        var options = new CliOptions();
        var args = new[] { "--coverage-report-formats", "html,json" };

        var result = OptionHandlers.HandleCoverageReportFormats(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.Coverage.ReportFormats).IsEqualTo("html,json");
    }

    [Test]
    public async Task HandleCoverageReportFormats_MissingValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--coverage-report-formats" };

        var result = OptionHandlers.HandleCoverageReportFormats(args, 0, options);

        await Assert.That(result.Error).Contains("--coverage-report-formats requires a value");
    }

    #endregion

    #region List Format Handler

    [Test]
    public async Task HandleListFormat_ValidFormat_SetsFormat()
    {
        var options = new CliOptions();
        var args = new[] { "--list-format", "tree" };

        var result = OptionHandlers.HandleListFormat(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.ListFormat).IsEqualTo(ListFormat.Tree);
    }

    [Test]
    public async Task HandleListFormat_InvalidFormat_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--list-format", "invalid" };

        var result = OptionHandlers.HandleListFormat(args, 0, options);

        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error).Contains("Unknown list format");
    }

    [Test]
    public async Task HandleListFormat_MissingValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--list-format" };

        var result = OptionHandlers.HandleListFormat(args, 0, options);

        await Assert.That(result.Error).Contains("--list-format requires a value");
    }

    #endregion

    #region Partition Handlers

    [Test]
    public async Task HandlePartition_MissingValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--partition" };

        var result = OptionHandlers.HandlePartition(args, 0, options);

        await Assert.That(result.Error).Contains("--partition requires a value");
    }

    [Test]
    public async Task HandlePartition_ValidValue_SetsPartition()
    {
        var options = new CliOptions();
        var args = new[] { "--partition", "4" };

        var result = OptionHandlers.HandlePartition(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.Partition.Total).IsEqualTo(4);
    }

    [Test]
    public async Task HandlePartition_ZeroValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--partition", "0" };

        var result = OptionHandlers.HandlePartition(args, 0, options);

        await Assert.That(result.Error).Contains("positive integer");
    }

    [Test]
    public async Task HandlePartition_NegativeValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--partition", "-1" };

        var result = OptionHandlers.HandlePartition(args, 0, options);

        await Assert.That(result.Error).Contains("positive integer");
    }

    [Test]
    public async Task HandlePartitionIndex_MissingValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--partition-index" };

        var result = OptionHandlers.HandlePartitionIndex(args, 0, options);

        await Assert.That(result.Error).Contains("--partition-index requires a value");
    }

    [Test]
    public async Task HandlePartitionIndex_ValidValue_SetsPartitionIndex()
    {
        var options = new CliOptions();
        var args = new[] { "--partition-index", "2" };

        var result = OptionHandlers.HandlePartitionIndex(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.Partition.Index).IsEqualTo(2);
    }

    [Test]
    public async Task HandlePartitionIndex_NegativeValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--partition-index", "-1" };

        var result = OptionHandlers.HandlePartitionIndex(args, 0, options);

        await Assert.That(result.Error).Contains("non-negative integer");
    }

    [Test]
    public async Task HandlePartitionIndex_NonInteger_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--partition-index", "abc" };

        var result = OptionHandlers.HandlePartitionIndex(args, 0, options);

        await Assert.That(result.Error).Contains("non-negative integer");
    }

    [Test]
    public async Task HandlePartitionStrategy_ValidValue_SetsStrategy()
    {
        var options = new CliOptions();
        var args = new[] { "--partition-strategy", "spec-count" };

        var result = OptionHandlers.HandlePartitionStrategy(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.Partition.Strategy).IsEqualTo(PartitionStrategy.SpecCount);
    }

    [Test]
    public async Task HandlePartitionStrategy_MissingValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--partition-strategy" };

        var result = OptionHandlers.HandlePartitionStrategy(args, 0, options);

        await Assert.That(result.Error).Contains("--partition-strategy requires a value");
    }

    [Test]
    public async Task HandlePartitionStrategy_InvalidValue_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--partition-strategy", "invalid" };

        var result = OptionHandlers.HandlePartitionStrategy(args, 0, options);

        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error).Contains("Unknown partition strategy");
    }

    #endregion

    #region Percentile Handler

    [Test]
    public async Task HandlePercentile_ValidValue_SetsPercentile()
    {
        var options = new CliOptions();
        var args = new[] { "--percentile", "95" };

        var result = OptionHandlers.HandlePercentile(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.Percentile).IsEqualTo(95);
    }

    [Test]
    public async Task HandlePercentile_TooLow_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--percentile", "0" };

        var result = OptionHandlers.HandlePercentile(args, 0, options);

        await Assert.That(result.Error).Contains("between 1 and 99");
    }

    [Test]
    public async Task HandlePercentile_TooHigh_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--percentile", "100" };

        var result = OptionHandlers.HandlePercentile(args, 0, options);

        await Assert.That(result.Error).Contains("between 1 and 99");
    }

    #endregion

    #region WindowSize Handler

    [Test]
    public async Task HandleWindowSize_ValidValue_SetsWindowSize()
    {
        var options = new CliOptions();
        var args = new[] { "--window-size", "10" };

        var result = OptionHandlers.HandleWindowSize(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.WindowSize).IsEqualTo(10);
    }

    [Test]
    public async Task HandleWindowSize_TooSmall_ReturnsError()
    {
        var options = new CliOptions();
        var args = new[] { "--window-size", "1" };

        var result = OptionHandlers.HandleWindowSize(args, 0, options);

        await Assert.That(result.Error).Contains("at least 2");
    }

    #endregion

    #region Docs Handlers

    [Test]
    public async Task HandleDocsFormat_Markdown_SetsDocsFormat()
    {
        var options = new CliOptions();
        var args = new[] { "--docs-format", "markdown" };

        var result = OptionHandlers.HandleDocsFormat(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.DocsFormat).IsEqualTo(DocsFormat.Markdown);
    }

    [Test]
    public async Task HandleDocsFormat_Html_SetsDocsFormat()
    {
        var options = new CliOptions();
        var args = new[] { "--docs-format", "html" };

        var result = OptionHandlers.HandleDocsFormat(args, 0, options);

        await Assert.That(options.DocsFormat).IsEqualTo(DocsFormat.Html);
    }

    [Test]
    public async Task HandleWithResults_SetsFlag()
    {
        var options = new CliOptions();

        var result = OptionHandlers.HandleWithResults([], 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(1);
        await Assert.That(options.WithResults).IsTrue();
    }

    [Test]
    public async Task HandleResultsFile_ValidPath_SetsPath()
    {
        var options = new CliOptions();
        var args = new[] { "--results-file", "/path/to/results.json" };

        var result = OptionHandlers.HandleResultsFile(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.ResultsFile).IsEqualTo("/path/to/results.json");
    }

    #endregion

    #region Coverage-Map Handlers

    [Test]
    public async Task HandleGapsOnly_SetsFlag()
    {
        var options = new CliOptions();

        var result = OptionHandlers.HandleGapsOnly([], 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(1);
        await Assert.That(options.GapsOnly).IsTrue();
    }

    [Test]
    public async Task HandleSpecs_ValidPath_SetsPath()
    {
        var options = new CliOptions();
        var args = new[] { "--specs", "./specs" };

        var result = OptionHandlers.HandleSpecs(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.CoverageMapSpecPath).IsEqualTo("./specs");
    }

    [Test]
    public async Task HandleNamespace_ValidFilter_SetsFilter()
    {
        var options = new CliOptions();
        var args = new[] { "--namespace", "MyApp.Services" };

        var result = OptionHandlers.HandleNamespace(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.CoverageMapNamespaceFilter).IsEqualTo("MyApp.Services");
    }

    [Test]
    public async Task HandleCoverageMapFormat_Console_SetsFormat()
    {
        var options = new CliOptions();
        var args = new[] { "--coverage-map-format", "console" };

        var result = OptionHandlers.HandleCoverageMapFormat(args, 0, options);

        await Assert.That(result.ConsumedArgs).IsEqualTo(2);
        await Assert.That(options.CoverageMapFormat).IsEqualTo(CoverageMapFormat.Console);
    }

    #endregion
}

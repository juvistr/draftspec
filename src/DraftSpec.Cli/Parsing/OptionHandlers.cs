using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Parsing;

/// <summary>
/// Static handler methods for individual command-line options.
/// Each handler returns an <see cref="OptionHandlerResult"/> indicating success/failure.
/// </summary>
public static class OptionHandlers
{
    #region Help

    public static OptionHandlerResult HandleHelp(string[] args, int i, CliOptions options)
    {
        options.ShowHelp = true;
        return OptionHandlerResult.Flag();
    }

    #endregion

    #region Format Options

    public static OptionHandlerResult HandleFormat(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--format requires a value (console, json, markdown, html, junit)");

        var formatValue = args[i + 1];
        if (!formatValue.TryParseOutputFormat(out var format))
            return OptionHandlerResult.Failed($"Unknown format: '{formatValue}'. Valid options: console, json, markdown, html, junit");

        options.Format = format;
        options.ExplicitlySet.Add(nameof(CliOptions.Format));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleOutput(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--output requires a file path");

        options.OutputFile = args[i + 1];
        options.ExplicitlySet.Add(nameof(CliOptions.OutputFile));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleCssUrl(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--css-url requires a URL");

        options.CssUrl = args[i + 1];
        return OptionHandlerResult.Value();
    }

    #endregion

    #region Run Flags

    public static OptionHandlerResult HandleForce(string[] args, int i, CliOptions options)
    {
        options.Force = true;
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleParallel(string[] args, int i, CliOptions options)
    {
        options.Parallel = true;
        options.ExplicitlySet.Add(nameof(CliOptions.Parallel));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleNoCache(string[] args, int i, CliOptions options)
    {
        options.NoCache = true;
        options.ExplicitlySet.Add(nameof(CliOptions.NoCache));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleBail(string[] args, int i, CliOptions options)
    {
        options.Bail = true;
        options.ExplicitlySet.Add(nameof(CliOptions.Bail));
        return OptionHandlerResult.Flag();
    }

    #endregion

    #region Filter Options

    public static OptionHandlerResult HandleFilterTags(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--filter-tags requires a value (comma-separated tags)");

        options.Filter.FilterTags = args[i + 1];
        options.ExplicitlySet.Add(nameof(options.Filter.FilterTags));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleExcludeTags(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--exclude-tags requires a value (comma-separated tags)");

        options.Filter.ExcludeTags = args[i + 1];
        options.ExplicitlySet.Add(nameof(options.Filter.ExcludeTags));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleFilterName(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--filter-name requires a value (regex pattern)");

        options.Filter.FilterName = args[i + 1];
        options.ExplicitlySet.Add(nameof(options.Filter.FilterName));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleExcludeName(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--exclude-name requires a value (regex pattern)");

        options.Filter.ExcludeName = args[i + 1];
        options.ExplicitlySet.Add(nameof(options.Filter.ExcludeName));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleContext(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--context requires a value (context pattern with / separator)");

        options.Filter.FilterContext ??= [];
        options.Filter.FilterContext.Add(args[i + 1]);
        options.ExplicitlySet.Add(nameof(options.Filter.FilterContext));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleExcludeContext(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--exclude-context requires a value (context pattern with / separator)");

        options.Filter.ExcludeContext ??= [];
        options.Filter.ExcludeContext.Add(args[i + 1]);
        options.ExplicitlySet.Add(nameof(options.Filter.ExcludeContext));
        return OptionHandlerResult.Value();
    }

    #endregion

    #region Coverage Options

    public static OptionHandlerResult HandleCoverage(string[] args, int i, CliOptions options)
    {
        options.Coverage.Enabled = true;
        options.ExplicitlySet.Add(nameof(options.Coverage.Enabled));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleCoverageOutput(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--coverage-output requires a directory path");

        options.Coverage.Output = args[i + 1];
        options.ExplicitlySet.Add(nameof(options.Coverage.Output));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleCoverageFormat(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--coverage-format requires a value (cobertura, xml, coverage)");

        var coverageFormatValue = args[i + 1];
        if (!coverageFormatValue.TryParseCoverageFormat(out var coverageFormat))
            return OptionHandlerResult.Failed($"Unknown coverage format: '{coverageFormatValue}'. Valid options: cobertura, xml, coverage");

        options.Coverage.Format = coverageFormat;
        options.ExplicitlySet.Add(nameof(options.Coverage.Format));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleCoverageReportFormats(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--coverage-report-formats requires a value (comma-separated: html, json)");

        options.Coverage.ReportFormats = args[i + 1].ToLowerInvariant();
        options.ExplicitlySet.Add(nameof(options.Coverage.ReportFormats));
        return OptionHandlerResult.Value();
    }

    #endregion

    #region List Command Options

    public static OptionHandlerResult HandleListFormat(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--list-format requires a value (tree, flat, json)");

        var listFormatValue = args[i + 1];
        if (!listFormatValue.TryParseListFormat(out var listFormat))
            return OptionHandlerResult.Failed($"Unknown list format: '{listFormatValue}'. Valid options: tree, flat, json");

        options.ListFormat = listFormat;
        options.ExplicitlySet.Add(nameof(CliOptions.ListFormat));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleShowLineNumbers(string[] args, int i, CliOptions options)
    {
        options.ShowLineNumbers = true;
        options.ExplicitlySet.Add(nameof(CliOptions.ShowLineNumbers));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleNoLineNumbers(string[] args, int i, CliOptions options)
    {
        options.ShowLineNumbers = false;
        options.ExplicitlySet.Add(nameof(CliOptions.ShowLineNumbers));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleFocusedOnly(string[] args, int i, CliOptions options)
    {
        options.FocusedOnly = true;
        options.ExplicitlySet.Add(nameof(CliOptions.FocusedOnly));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandlePendingOnly(string[] args, int i, CliOptions options)
    {
        options.PendingOnly = true;
        options.ExplicitlySet.Add(nameof(CliOptions.PendingOnly));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleSkippedOnly(string[] args, int i, CliOptions options)
    {
        options.SkippedOnly = true;
        options.ExplicitlySet.Add(nameof(CliOptions.SkippedOnly));
        return OptionHandlerResult.Flag();
    }

    #endregion

    #region Validate Command Options

    public static OptionHandlerResult HandleStatic(string[] args, int i, CliOptions options)
    {
        options.Static = true;
        options.ExplicitlySet.Add(nameof(CliOptions.Static));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleStrict(string[] args, int i, CliOptions options)
    {
        options.Strict = true;
        options.ExplicitlySet.Add(nameof(CliOptions.Strict));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleQuiet(string[] args, int i, CliOptions options)
    {
        options.Quiet = true;
        options.ExplicitlySet.Add(nameof(CliOptions.Quiet));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleFiles(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--files requires a value (comma-separated file paths)");

        var filesArg = args[i + 1];
        options.Files = filesArg.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();
        options.ExplicitlySet.Add(nameof(CliOptions.Files));
        return OptionHandlerResult.Value();
    }

    #endregion

    #region Statistics Options

    public static OptionHandlerResult HandleNoStats(string[] args, int i, CliOptions options)
    {
        options.NoStats = true;
        options.ExplicitlySet.Add(nameof(CliOptions.NoStats));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleStatsOnly(string[] args, int i, CliOptions options)
    {
        options.StatsOnly = true;
        options.ExplicitlySet.Add(nameof(CliOptions.StatsOnly));
        return OptionHandlerResult.Flag();
    }

    #endregion

    #region Partition Options

    public static OptionHandlerResult HandlePartition(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--partition requires a value (total number of partitions)");

        if (!int.TryParse(args[i + 1], out var partition) || partition < 1)
            return OptionHandlerResult.Failed("--partition must be a positive integer");

        options.Partition.Total = partition;
        options.ExplicitlySet.Add(nameof(options.Partition.Total));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandlePartitionIndex(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--partition-index requires a value (0-based index)");

        if (!int.TryParse(args[i + 1], out var index) || index < 0)
            return OptionHandlerResult.Failed("--partition-index must be a non-negative integer");

        options.Partition.Index = index;
        options.ExplicitlySet.Add(nameof(options.Partition.Index));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandlePartitionStrategy(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--partition-strategy requires a value (file, spec-count)");

        var strategyValue = args[i + 1];
        if (!strategyValue.TryParsePartitionStrategy(out var strategy))
            return OptionHandlerResult.Failed($"Unknown partition strategy: '{strategyValue}'. Valid options: file, spec-count");

        options.Partition.Strategy = strategy;
        options.ExplicitlySet.Add(nameof(options.Partition.Strategy));
        return OptionHandlerResult.Value();
    }

    #endregion

    #region Watch Command Options

    public static OptionHandlerResult HandleIncremental(string[] args, int i, CliOptions options)
    {
        options.Incremental = true;
        options.ExplicitlySet.Add(nameof(CliOptions.Incremental));
        return OptionHandlerResult.Flag();
    }

    #endregion

    #region Impact Analysis Options

    public static OptionHandlerResult HandleAffectedBy(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--affected-by requires a value (commit ref, 'staged', or file path)");

        options.AffectedBy = args[i + 1];
        options.ExplicitlySet.Add(nameof(CliOptions.AffectedBy));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleDryRun(string[] args, int i, CliOptions options)
    {
        options.DryRun = true;
        options.ExplicitlySet.Add(nameof(CliOptions.DryRun));
        return OptionHandlerResult.Flag();
    }

    #endregion

    #region Flaky Test Options

    public static OptionHandlerResult HandleQuarantine(string[] args, int i, CliOptions options)
    {
        options.Quarantine = true;
        options.ExplicitlySet.Add(nameof(CliOptions.Quarantine));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleNoHistory(string[] args, int i, CliOptions options)
    {
        options.NoHistory = true;
        options.ExplicitlySet.Add(nameof(CliOptions.NoHistory));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleInteractive(string[] args, int i, CliOptions options)
    {
        options.Interactive = true;
        options.ExplicitlySet.Add(nameof(CliOptions.Interactive));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleMinChanges(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--min-changes requires a value (minimum status changes)");

        if (!int.TryParse(args[i + 1], out var minChanges) || minChanges < 1)
            return OptionHandlerResult.Failed("--min-changes must be a positive integer");

        options.MinStatusChanges = minChanges;
        options.ExplicitlySet.Add(nameof(CliOptions.MinStatusChanges));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleWindowSize(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--window-size requires a value (number of runs to analyze)");

        if (!int.TryParse(args[i + 1], out var windowSize) || windowSize < 2)
            return OptionHandlerResult.Failed("--window-size must be at least 2");

        options.WindowSize = windowSize;
        options.ExplicitlySet.Add(nameof(CliOptions.WindowSize));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleClear(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--clear requires a spec ID to clear");

        options.Clear = args[i + 1];
        options.ExplicitlySet.Add(nameof(CliOptions.Clear));
        return OptionHandlerResult.Value();
    }

    #endregion

    #region Estimate Command Options

    public static OptionHandlerResult HandlePercentile(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--percentile requires a value (1-99)");

        if (!int.TryParse(args[i + 1], out var percentile) || percentile < 1 || percentile > 99)
            return OptionHandlerResult.Failed("--percentile must be between 1 and 99");

        options.Percentile = percentile;
        options.ExplicitlySet.Add(nameof(CliOptions.Percentile));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleOutputSeconds(string[] args, int i, CliOptions options)
    {
        options.OutputSeconds = true;
        options.ExplicitlySet.Add(nameof(CliOptions.OutputSeconds));
        return OptionHandlerResult.Flag();
    }

    #endregion

    #region Docs Command Options

    public static OptionHandlerResult HandleDocsFormat(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--docs-format requires a value (markdown, html)");

        var docsFormatValue = args[i + 1];
        if (!docsFormatValue.TryParseDocsFormat(out var docsFormat))
            return OptionHandlerResult.Failed($"Unknown docs format: '{docsFormatValue}'. Valid options: markdown, html");

        options.DocsFormat = docsFormat;
        options.ExplicitlySet.Add(nameof(CliOptions.DocsFormat));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleDocsContext(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--docs-context requires a value (context filter pattern)");

        options.DocsContext = args[i + 1];
        options.ExplicitlySet.Add(nameof(CliOptions.DocsContext));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleWithResults(string[] args, int i, CliOptions options)
    {
        options.WithResults = true;
        options.ExplicitlySet.Add(nameof(CliOptions.WithResults));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleResultsFile(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--results-file requires a path to a JSON results file");

        options.ResultsFile = args[i + 1];
        options.ExplicitlySet.Add(nameof(CliOptions.ResultsFile));
        return OptionHandlerResult.Value();
    }

    #endregion

    #region Coverage-Map Command Options

    public static OptionHandlerResult HandleGapsOnly(string[] args, int i, CliOptions options)
    {
        options.GapsOnly = true;
        options.ExplicitlySet.Add(nameof(CliOptions.GapsOnly));
        return OptionHandlerResult.Flag();
    }

    public static OptionHandlerResult HandleSpecs(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--specs requires a path to spec files");

        options.CoverageMapSpecPath = args[i + 1];
        options.ExplicitlySet.Add(nameof(CliOptions.CoverageMapSpecPath));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleNamespace(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--namespace requires a value (comma-separated namespaces)");

        options.CoverageMapNamespaceFilter = args[i + 1];
        options.ExplicitlySet.Add(nameof(CliOptions.CoverageMapNamespaceFilter));
        return OptionHandlerResult.Value();
    }

    public static OptionHandlerResult HandleCoverageMapFormat(string[] args, int i, CliOptions options)
    {
        if (i + 1 >= args.Length)
            return OptionHandlerResult.Failed("--coverage-map-format requires a value (console, json)");

        var formatValue = args[i + 1];
        if (!formatValue.TryParseCoverageMapFormat(out var cmFormat))
            return OptionHandlerResult.Failed($"Unknown coverage-map format: '{formatValue}'. Valid options: console, json");

        options.CoverageMapFormat = cmFormat;
        options.ExplicitlySet.Add(nameof(CliOptions.CoverageMapFormat));
        return OptionHandlerResult.Value();
    }

    #endregion
}

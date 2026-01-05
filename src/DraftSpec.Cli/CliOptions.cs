using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli;

public class CliOptions
{
    /// <summary>
    /// Tracks which options were explicitly set via CLI (vs defaults).
    /// Used to determine if config file values should apply.
    /// </summary>
    public HashSet<string> ExplicitlySet { get; } = [];

    #region Core Options

    public string Command { get; set; } = "";
    public string Path { get; set; } = ".";
    public OutputFormat Format { get; set; } = OutputFormat.Console;
    public string? OutputFile { get; set; }
    public string? CssUrl { get; set; }
    public bool ShowHelp { get; set; }
    public string? Error { get; set; }
    public bool Force { get; set; }
    public string? SpecName { get; set; }
    public bool Parallel { get; set; }

    /// <summary>
    /// Additional reporter names to use (comma-separated).
    /// Example: "file:results.json,slack"
    /// </summary>
    public string? Reporters { get; set; }

    /// <summary>
    /// Disable dotnet-script caching, forcing recompilation on every run.
    /// </summary>
    public bool NoCache { get; set; }

    /// <summary>
    /// Stop execution after first spec failure.
    /// Remaining specs will be reported as skipped.
    /// </summary>
    public bool Bail { get; set; }

    #endregion

    #region Composed Option Groups

    /// <summary>
    /// Filter options for selecting which specs to run.
    /// Used by run, list, watch, and docs commands.
    /// </summary>
    public FilterOptions Filter { get; } = new();

    /// <summary>
    /// Coverage options for code coverage collection.
    /// Used by run command.
    /// </summary>
    public CoverageOptions Coverage { get; } = new();

    /// <summary>
    /// Partition options for CI parallelism.
    /// Used by run command.
    /// </summary>
    public PartitionOptions Partition { get; } = new();

    #endregion

    // List command options

    /// <summary>
    /// Output format for the list command: tree, flat, or json.
    /// Default: tree
    /// </summary>
    public ListFormat ListFormat { get; set; } = ListFormat.Tree;

    /// <summary>
    /// Show line numbers in list output.
    /// Default: true
    /// </summary>
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>
    /// Show only focused specs (fit()) in list output.
    /// </summary>
    public bool FocusedOnly { get; set; }

    /// <summary>
    /// Show only pending specs (specs without body) in list output.
    /// </summary>
    public bool PendingOnly { get; set; }

    /// <summary>
    /// Show only skipped specs (xit()) in list output.
    /// </summary>
    public bool SkippedOnly { get; set; }

    // Validate command options

    /// <summary>
    /// Use static parsing only (no script execution).
    /// This is the default for validate command; flag is for documentation.
    /// </summary>
    public bool Static { get; set; }

    /// <summary>
    /// Treat warnings as errors (exit code 2 instead of 0).
    /// Used with validate command.
    /// </summary>
    public bool Strict { get; set; }

    /// <summary>
    /// Show only errors, suppress progress and warnings.
    /// Used with validate command.
    /// </summary>
    public bool Quiet { get; set; }

    /// <summary>
    /// Specific files to validate (for pre-commit hooks).
    /// When set, only these files are validated instead of scanning directory.
    /// </summary>
    public List<string>? Files { get; set; }

    // Run command statistics options

    /// <summary>
    /// Disable pre-run statistics display.
    /// By default, stats are shown before running specs.
    /// </summary>
    public bool NoStats { get; set; }

    /// <summary>
    /// Show spec statistics only, without running specs.
    /// Displays discovered spec counts and exits.
    /// </summary>
    public bool StatsOnly { get; set; }

    // Watch command options

    /// <summary>
    /// Enable incremental watch mode (only re-run changed specs).
    /// When disabled, entire files are re-run on any change.
    /// </summary>
    public bool Incremental { get; set; }

    // Test impact analysis options

    /// <summary>
    /// Run only specs affected by changes since the specified reference.
    /// Can be: "staged", a commit ref (e.g., "HEAD~1", "main"), or a file path containing changed files.
    /// </summary>
    public string? AffectedBy { get; set; }

    /// <summary>
    /// Show which specs would run without actually running them.
    /// Used with --affected-by to preview impacted specs.
    /// </summary>
    public bool DryRun { get; set; }

    // Flaky test detection options

    /// <summary>
    /// Skip known flaky tests during execution.
    /// Flaky specs are identified from execution history.
    /// </summary>
    public bool Quarantine { get; set; }

    /// <summary>
    /// Disable recording of test results to history.
    /// By default, results are saved to .draftspec/history.json.
    /// </summary>
    public bool NoHistory { get; set; }

    /// <summary>
    /// Enable interactive spec selection before running.
    /// When enabled, displays a multi-select UI for choosing specs.
    /// </summary>
    public bool Interactive { get; set; }

    // Flaky command options

    /// <summary>
    /// Minimum status changes to be considered flaky.
    /// Default: 2
    /// </summary>
    public int MinStatusChanges { get; set; } = 2;

    /// <summary>
    /// Number of recent runs to analyze for flakiness.
    /// Default: 10
    /// </summary>
    public int WindowSize { get; set; } = 10;

    /// <summary>
    /// Spec ID to clear from history.
    /// Used with flaky command.
    /// </summary>
    public string? Clear { get; set; }

    // Estimate command options

    /// <summary>
    /// Percentile to use for runtime estimation (1-99).
    /// Default: 50 (median)
    /// </summary>
    public int Percentile { get; set; } = 50;

    /// <summary>
    /// Output the estimate in seconds (machine-readable format).
    /// Used with estimate command.
    /// </summary>
    public bool OutputSeconds { get; set; }

    // Cache command options

    /// <summary>
    /// Cache subcommand: stats, clear.
    /// Used with cache command.
    /// </summary>
    public string CacheSubcommand { get; set; } = "stats";

    // Docs command options

    /// <summary>
    /// Output format for the docs command: markdown or html.
    /// Default: markdown
    /// </summary>
    public DocsFormat DocsFormat { get; set; } = DocsFormat.Markdown;

    /// <summary>
    /// Filter to a specific describe/context block.
    /// </summary>
    public string? DocsContext { get; set; }

    /// <summary>
    /// Include test results from a previous run.
    /// </summary>
    public bool WithResults { get; set; }

    /// <summary>
    /// Path to JSON results file (used with --with-results).
    /// </summary>
    public string? ResultsFile { get; set; }

    // Coverage-map command options

    /// <summary>
    /// Path to source files to analyze for coverage mapping.
    /// </summary>
    public string? CoverageMapSourcePath { get; set; }

    /// <summary>
    /// Path to spec files for coverage mapping.
    /// </summary>
    public string? CoverageMapSpecPath { get; set; }

    /// <summary>
    /// Output format for the coverage-map command: console or json.
    /// Default: console
    /// </summary>
    public CoverageMapFormat CoverageMapFormat { get; set; } = CoverageMapFormat.Console;

    /// <summary>
    /// Show only uncovered methods (gaps) in coverage-map output.
    /// </summary>
    public bool GapsOnly { get; set; }

    /// <summary>
    /// Filter coverage-map to specific namespaces (comma-separated).
    /// </summary>
    public string? CoverageMapNamespaceFilter { get; set; }


    /// <summary>
    /// Apply default values from a project configuration file.
    /// Only applies values that weren't explicitly set via CLI.
    /// </summary>
    /// <param name="config">The project configuration to apply.</param>
    public void ApplyDefaults(DraftSpecProjectConfig config)
    {
        ExplicitlySet.ApplyIfNotSet(nameof(Parallel), v => Parallel = v, config.Parallel);
        ExplicitlySet.ApplyIfNotSet(nameof(Bail), v => Bail = v, config.Bail);
        ExplicitlySet.ApplyIfNotSet(nameof(NoCache), v => NoCache = v, config.NoCache);

        ExplicitlySet.ApplyIfValid<OutputFormat>(nameof(Format), v => Format = v, config.Format,
            (string s, out OutputFormat r) => s.TryParseOutputFormat(out r));

        ExplicitlySet.ApplyIfNotEmpty(nameof(OutputFile), v => OutputFile = v, config.OutputDirectory);
        ExplicitlySet.ApplyIfNotEmpty(nameof(Reporters), v => Reporters = v, config.Reporters);

        // Filter configuration
        ExplicitlySet.ApplyIfNotEmpty(nameof(Filter.FilterTags), v => Filter.FilterTags = v, config.Tags?.Include);
        ExplicitlySet.ApplyIfNotEmpty(nameof(Filter.ExcludeTags), v => Filter.ExcludeTags = v, config.Tags?.Exclude);

        // Coverage configuration
        ExplicitlySet.ApplyIfTrue(nameof(Coverage.Enabled), v => Coverage.Enabled = v, config.Coverage?.Enabled);
        ExplicitlySet.ApplyIfNotEmpty(nameof(Coverage.Output), v => Coverage.Output = v, config.Coverage?.Output);
        ExplicitlySet.ApplyIfValid<CoverageFormat>(nameof(Coverage.Format), v => Coverage.Format = v,
            config.Coverage?.Format, (string s, out CoverageFormat r) => s.TryParseCoverageFormat(out r));
        ExplicitlySet.ApplyIfNotEmpty(nameof(Coverage.ReportFormats), v => Coverage.ReportFormats = v,
            config.Coverage?.ReportFormats);
    }

    #region Conversion Methods

    /// <summary>
    /// Converts to RunOptions for the run command.
    /// </summary>
    public RunOptions ToRunOptions()
    {
        // Copy SpecName to Filter if set (for run/list commands)
        if (!string.IsNullOrEmpty(SpecName))
            Filter.SpecName = SpecName;

        return new RunOptions
        {
            Path = Path,
            Format = Format,
            OutputFile = OutputFile,
            CssUrl = CssUrl,
            Parallel = Parallel,
            NoCache = NoCache,
            Bail = Bail,
            NoStats = NoStats,
            StatsOnly = StatsOnly,
            Reporters = Reporters,
            Filter = Filter,
            Coverage = Coverage,
            Partition = Partition,
            AffectedBy = AffectedBy,
            DryRun = DryRun,
            Quarantine = Quarantine,
            NoHistory = NoHistory,
            Interactive = Interactive
        };
    }

    /// <summary>
    /// Converts to ListOptions for the list command.
    /// </summary>
    public ListOptions ToListOptions()
    {
        // Copy SpecName to Filter if set (for run/list commands)
        if (!string.IsNullOrEmpty(SpecName))
            Filter.SpecName = SpecName;

        return new ListOptions
        {
            Path = Path,
            Format = ListFormat,
            ShowLineNumbers = ShowLineNumbers,
            FocusedOnly = FocusedOnly,
            PendingOnly = PendingOnly,
            SkippedOnly = SkippedOnly,
            Filter = Filter
        };
    }

    /// <summary>
    /// Converts to ValidateOptions for the validate command.
    /// </summary>
    public ValidateOptions ToValidateOptions() => new()
    {
        Path = Path,
        Static = Static,
        Strict = Strict,
        Quiet = Quiet,
        Files = Files
    };

    /// <summary>
    /// Converts to WatchOptions for the watch command.
    /// </summary>
    public WatchOptions ToWatchOptions() => new()
    {
        Path = Path,
        Format = Format,
        Incremental = Incremental,
        Parallel = Parallel,
        NoCache = NoCache,
        Bail = Bail,
        Filter = Filter
    };

    /// <summary>
    /// Converts to InitOptions for the init command.
    /// </summary>
    public InitOptions ToInitOptions() => new()
    {
        Path = Path,
        Force = Force
    };

    /// <summary>
    /// Converts to NewOptions for the new command.
    /// </summary>
    public NewOptions ToNewOptions() => new()
    {
        Path = Path,
        SpecName = SpecName
    };

    /// <summary>
    /// Converts to SchemaOptions for the schema command.
    /// </summary>
    public SchemaOptions ToSchemaOptions() => new()
    {
        OutputFile = OutputFile
    };

    /// <summary>
    /// Converts to FlakyOptions for the flaky command.
    /// </summary>
    public FlakyOptions ToFlakyOptions() => new()
    {
        Path = Path,
        MinStatusChanges = MinStatusChanges,
        WindowSize = WindowSize,
        Clear = Clear
    };

    /// <summary>
    /// Converts to EstimateOptions for the estimate command.
    /// </summary>
    public EstimateOptions ToEstimateOptions() => new()
    {
        Path = Path,
        Percentile = Percentile,
        OutputSeconds = OutputSeconds
    };

    /// <summary>
    /// Converts to CacheOptions for the cache command.
    /// </summary>
    public CacheOptions ToCacheOptions() => new()
    {
        Subcommand = CacheSubcommand,
        Path = Path
    };

    /// <summary>
    /// Converts to DocsOptions for the docs command.
    /// </summary>
    public DocsOptions ToDocsOptions() => new()
    {
        Path = Path,
        Format = DocsFormat,
        Context = DocsContext,
        WithResults = WithResults,
        ResultsFile = ResultsFile,
        Filter = Filter
    };

    public CoverageMapOptions ToCoverageMapOptions() => new()
    {
        SourcePath = CoverageMapSourcePath ?? Path,
        SpecPath = CoverageMapSpecPath,
        Format = CoverageMapFormat,
        GapsOnly = GapsOnly,
        NamespaceFilter = CoverageMapNamespaceFilter
    };

    #endregion
}

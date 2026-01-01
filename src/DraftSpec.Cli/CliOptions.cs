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

    /// <summary>
    /// Comma-separated list of tags to include.
    /// Only specs with any of these tags will run.
    /// </summary>
    public string? FilterTags { get; set; }

    /// <summary>
    /// Comma-separated list of tags to exclude.
    /// Specs with any of these tags will be skipped.
    /// </summary>
    public string? ExcludeTags { get; set; }

    /// <summary>
    /// Regex pattern to match spec names (context path + description).
    /// Only specs matching this pattern will run.
    /// </summary>
    public string? FilterName { get; set; }

    /// <summary>
    /// Regex pattern to exclude spec names (context path + description).
    /// Specs matching this pattern will be skipped.
    /// </summary>
    public string? ExcludeName { get; set; }

    /// <summary>
    /// Context patterns to include (glob-style with / separator).
    /// Only specs within matching contexts will run.
    /// Supports * (single segment) and ** (multiple segments).
    /// Example: "UserService/CreateAsync", "*/CreateAsync", "Integration/**"
    /// </summary>
    public List<string>? FilterContext { get; set; }

    /// <summary>
    /// Context patterns to exclude (glob-style with / separator).
    /// Specs within matching contexts will be skipped.
    /// Supports * (single segment) and ** (multiple segments).
    /// Example: "Legacy/*", "**/Slow"
    /// </summary>
    public List<string>? ExcludeContext { get; set; }

    /// <summary>
    /// Enable code coverage collection via dotnet-coverage.
    /// </summary>
    public bool Coverage { get; set; }

    /// <summary>
    /// Output directory for coverage reports.
    /// Default: ./coverage
    /// </summary>
    public string? CoverageOutput { get; set; }

    /// <summary>
    /// Coverage output format: cobertura, xml, or coverage.
    /// Default: cobertura
    /// </summary>
    public CoverageFormat CoverageFormat { get; set; } = CoverageFormat.Cobertura;

    /// <summary>
    /// Additional coverage report formats to generate (comma-separated).
    /// Options: html, json
    /// Example: "html,json" generates both HTML and JSON reports.
    /// </summary>
    public string? CoverageReportFormats { get; set; }

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

    // Run command line filtering

    /// <summary>
    /// Line number filters parsed from file:line syntax.
    /// Used to run specific specs by line number (e.g., "file.spec.csx:15,23").
    /// </summary>
    public List<LineFilter>? LineFilters { get; set; }

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

    // Partitioning options for CI parallelism

    /// <summary>
    /// Total number of partitions to divide specs into.
    /// Used with --partition-index for CI parallel execution.
    /// </summary>
    public int? Partition { get; set; }

    /// <summary>
    /// Zero-based index of this partition (0 to Partition-1).
    /// </summary>
    public int? PartitionIndex { get; set; }

    /// <summary>
    /// Strategy for partitioning: "file" (default) or "spec-count".
    /// - file: Round-robin by sorted file path (fast, deterministic)
    /// - spec-count: Balance by spec count per file (requires parsing)
    /// </summary>
    public PartitionStrategy PartitionStrategy { get; set; } = PartitionStrategy.File;

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
        ExplicitlySet.ApplyIfNotEmpty(nameof(FilterTags), v => FilterTags = v, config.Tags?.Include);
        ExplicitlySet.ApplyIfNotEmpty(nameof(ExcludeTags), v => ExcludeTags = v, config.Tags?.Exclude);
        ExplicitlySet.ApplyIfNotEmpty(nameof(Reporters), v => Reporters = v, config.Reporters);

        // Coverage configuration
        ExplicitlySet.ApplyIfTrue(nameof(Coverage), v => Coverage = v, config.Coverage?.Enabled);
        ExplicitlySet.ApplyIfNotEmpty(nameof(CoverageOutput), v => CoverageOutput = v, config.Coverage?.Output);
        ExplicitlySet.ApplyIfValid<CoverageFormat>(nameof(CoverageFormat), v => CoverageFormat = v,
            config.Coverage?.Format, (string s, out CoverageFormat r) => s.TryParseCoverageFormat(out r));
        ExplicitlySet.ApplyIfNotEmpty(nameof(CoverageReportFormats), v => CoverageReportFormats = v,
            config.Coverage?.ReportFormats);
    }

    #region Conversion Methods

    /// <summary>
    /// Converts to RunOptions for the run command.
    /// </summary>
    public RunOptions ToRunOptions() => new()
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
        Filter = ToFilterOptions(),
        Coverage = ToCoverageOptions(),
        Partition = ToPartitionOptions(),
        AffectedBy = AffectedBy,
        DryRun = DryRun,
        Quarantine = Quarantine,
        NoHistory = NoHistory
    };

    /// <summary>
    /// Converts to ListOptions for the list command.
    /// </summary>
    public ListOptions ToListOptions() => new()
    {
        Path = Path,
        Format = ListFormat,
        ShowLineNumbers = ShowLineNumbers,
        FocusedOnly = FocusedOnly,
        PendingOnly = PendingOnly,
        SkippedOnly = SkippedOnly,
        Filter = ToFilterOptions()
    };

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
        Filter = ToFilterOptions()
    };

    /// <summary>
    /// Creates FilterOptions from filter-related properties.
    /// </summary>
    private FilterOptions ToFilterOptions() => new()
    {
        SpecName = SpecName,
        FilterTags = FilterTags,
        ExcludeTags = ExcludeTags,
        FilterName = FilterName,
        ExcludeName = ExcludeName,
        FilterContext = FilterContext,
        ExcludeContext = ExcludeContext,
        LineFilters = LineFilters
    };

    /// <summary>
    /// Creates CoverageOptions from coverage-related properties.
    /// </summary>
    private CoverageOptions ToCoverageOptions() => new()
    {
        Enabled = Coverage,
        Output = CoverageOutput,
        Format = CoverageFormat,
        ReportFormats = CoverageReportFormats
    };

    /// <summary>
    /// Creates PartitionOptions from partitioning-related properties.
    /// </summary>
    private PartitionOptions ToPartitionOptions() => new()
    {
        Total = Partition,
        Index = PartitionIndex,
        Strategy = PartitionStrategy
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

    #endregion
}

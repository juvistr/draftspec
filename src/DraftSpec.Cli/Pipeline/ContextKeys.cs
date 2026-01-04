namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Well-known keys for <see cref="CommandContext.Items"/> dictionary.
/// Use these constants for phase-to-phase communication to ensure consistency.
/// </summary>
/// <remarks>
/// Each phase documents which keys it requires and produces. See individual
/// phase documentation for the expected types and semantics of each key.
/// </remarks>
public static class ContextKeys
{
    /// <summary>
    /// Resolved absolute path to the project/spec directory.
    /// Type: <see cref="string"/>
    /// </summary>
    public const string ProjectPath = nameof(ProjectPath);

    /// <summary>
    /// List of discovered spec file paths.
    /// Type: <c>IReadOnlyList&lt;string&gt;</c>
    /// </summary>
    public const string SpecFiles = nameof(SpecFiles);

    /// <summary>
    /// Parsed spec results from spec files.
    /// Type: <c>IReadOnlyDictionary&lt;string, StaticParseResult&gt;</c>
    /// </summary>
    public const string ParsedSpecs = nameof(ParsedSpecs);

    /// <summary>
    /// Whether quarantine mode is enabled.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string Quarantine = nameof(Quarantine);

    /// <summary>
    /// Active filter options for spec selection.
    /// Type: <c>FilterOptions</c>
    /// </summary>
    public const string Filter = nameof(Filter);

    /// <summary>
    /// Results from spec execution.
    /// Type: <c>IReadOnlyList&lt;SpecResult&gt;</c>
    /// </summary>
    public const string RunResults = nameof(RunResults);

    /// <summary>
    /// Discovered specs after filtering.
    /// Type: <c>IReadOnlyList&lt;DiscoveredSpec&gt;</c>
    /// </summary>
    public const string FilteredSpecs = nameof(FilteredSpecs);

    /// <summary>
    /// Errors encountered during spec discovery/parsing.
    /// Type: <c>IReadOnlyList&lt;DiscoveryError&gt;</c>
    /// </summary>
    public const string DiscoveryErrors = nameof(DiscoveryErrors);

    /// <summary>
    /// List command output format.
    /// Type: <c>ListFormat</c>
    /// </summary>
    public const string ListFormat = nameof(ListFormat);

    /// <summary>
    /// Whether to show line numbers in list output.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string ShowLineNumbers = nameof(ShowLineNumbers);

    /// <summary>
    /// Show only focused specs filter.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string FocusedOnly = nameof(FocusedOnly);

    /// <summary>
    /// Show only pending specs filter.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string PendingOnly = nameof(PendingOnly);

    /// <summary>
    /// Show only skipped specs filter.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string SkippedOnly = nameof(SkippedOnly);

    #region Docs Command Keys

    /// <summary>
    /// Docs command output format.
    /// Type: <c>DocsFormat</c>
    /// </summary>
    public const string DocsFormat = nameof(DocsFormat);

    /// <summary>
    /// Context filter pattern for docs command.
    /// Type: <see cref="string"/>
    /// </summary>
    public const string ContextFilter = nameof(ContextFilter);

    /// <summary>
    /// Whether to include results in docs output.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string WithResults = nameof(WithResults);

    /// <summary>
    /// Path to results file for docs command.
    /// Type: <see cref="string"/>
    /// </summary>
    public const string ResultsFile = nameof(ResultsFile);

    #endregion

    #region Validate Command Keys

    /// <summary>
    /// Explicit list of spec files to validate.
    /// Type: <c>IReadOnlyList&lt;string&gt;</c>
    /// </summary>
    public const string ExplicitFiles = nameof(ExplicitFiles);

    /// <summary>
    /// Whether to treat warnings as errors.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string Strict = nameof(Strict);

    /// <summary>
    /// Whether to suppress non-error output.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string Quiet = nameof(Quiet);

    /// <summary>
    /// Validation results from ValidationPhase.
    /// Type: <c>IReadOnlyList&lt;FileValidationResult&gt;</c>
    /// </summary>
    public const string ValidationResults = nameof(ValidationResults);

    #endregion

    #region CoverageMap Command Keys

    /// <summary>
    /// Path to source files or directory to analyze.
    /// Type: <see cref="string"/>
    /// </summary>
    public const string SourcePath = nameof(SourcePath);

    /// <summary>
    /// Path to spec files or directory.
    /// Type: <see cref="string"/>
    /// </summary>
    public const string SpecPath = nameof(SpecPath);

    /// <summary>
    /// List of discovered C# source files.
    /// Type: <c>IReadOnlyList&lt;string&gt;</c>
    /// </summary>
    public const string SourceFiles = nameof(SourceFiles);

    /// <summary>
    /// Coverage map output format.
    /// Type: <c>CoverageMapFormat</c>
    /// </summary>
    public const string CoverageMapFormat = nameof(CoverageMapFormat);

    /// <summary>
    /// Whether to show only uncovered methods.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string GapsOnly = nameof(GapsOnly);

    /// <summary>
    /// Namespace filter pattern (comma-separated).
    /// Type: <see cref="string"/>
    /// </summary>
    public const string NamespaceFilter = nameof(NamespaceFilter);

    /// <summary>
    /// Coverage mapping results.
    /// Type: <c>CoverageMapResult</c>
    /// </summary>
    public const string CoverageMapResult = nameof(CoverageMapResult);

    #endregion

    #region New Command Keys

    /// <summary>
    /// Name for the new spec file (without extension).
    /// Type: <see cref="string"/>
    /// </summary>
    public const string SpecName = nameof(SpecName);

    #endregion

    #region Init Command Keys

    /// <summary>
    /// Whether to overwrite existing files.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string Force = nameof(Force);

    /// <summary>
    /// Discovered project information from .csproj.
    /// Type: <c>ProjectInfo?</c> (nullable - may not find project)
    /// </summary>
    public const string ProjectInfo = nameof(ProjectInfo);

    #endregion

    #region Schema Command Keys

    /// <summary>
    /// Output file path for schema command.
    /// Type: <see cref="string"/> (nullable - writes to stdout if null)
    /// </summary>
    public const string OutputFile = nameof(OutputFile);

    #endregion

    #region Cache Command Keys

    /// <summary>
    /// Cache subcommand to execute (stats, clear).
    /// Type: <see cref="string"/>
    /// </summary>
    public const string CacheSubcommand = nameof(CacheSubcommand);

    #endregion

    #region History Command Keys

    /// <summary>
    /// Loaded spec execution history.
    /// Type: <c>SpecHistory</c>
    /// </summary>
    public const string History = nameof(History);

    #endregion

    #region Estimate Command Keys

    /// <summary>
    /// Percentile for runtime estimation (1-99).
    /// Type: <see cref="int"/>
    /// </summary>
    public const string Percentile = nameof(Percentile);

    /// <summary>
    /// Whether to output seconds only (machine-readable).
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string OutputSeconds = nameof(OutputSeconds);

    #endregion

    #region Flaky Command Keys

    /// <summary>
    /// Minimum status changes to consider a spec flaky.
    /// Type: <see cref="int"/>
    /// </summary>
    public const string MinStatusChanges = nameof(MinStatusChanges);

    /// <summary>
    /// Number of recent runs to analyze for flakiness.
    /// Type: <see cref="int"/>
    /// </summary>
    public const string WindowSize = nameof(WindowSize);

    /// <summary>
    /// Spec ID to clear from history (nullable).
    /// Type: <see cref="string"/>
    /// </summary>
    public const string Clear = nameof(Clear);

    #endregion
}

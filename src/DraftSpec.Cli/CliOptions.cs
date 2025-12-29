using DraftSpec.Cli.Configuration;

namespace DraftSpec.Cli;

/// <summary>
/// Represents a line number filter for running specs at specific lines.
/// </summary>
/// <param name="File">The spec file path.</param>
/// <param name="Lines">The line numbers to run.</param>
public record LineFilter(string File, int[] Lines);

public class CliOptions
{
    /// <summary>
    /// Tracks which options were explicitly set via CLI (vs defaults).
    /// Used to determine if config file values should apply.
    /// </summary>
    public HashSet<string> ExplicitlySet { get; } = [];

    public string Command { get; set; } = "";
    public string Path { get; set; } = ".";
    public string Format { get; set; } = "console";
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
    public string CoverageFormat { get; set; } = "cobertura";

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
    public string ListFormat { get; set; } = "tree";

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

    /// <summary>
    /// Apply default values from a project configuration file.
    /// Only applies values that weren't explicitly set via CLI.
    /// </summary>
    /// <param name="config">The project configuration to apply.</param>
    public void ApplyDefaults(DraftSpecProjectConfig config)
    {
        if (!ExplicitlySet.Contains(nameof(Parallel)) && config.Parallel.HasValue)
            Parallel = config.Parallel.Value;

        if (!ExplicitlySet.Contains(nameof(Bail)) && config.Bail.HasValue)
            Bail = config.Bail.Value;

        if (!ExplicitlySet.Contains(nameof(NoCache)) && config.NoCache.HasValue)
            NoCache = config.NoCache.Value;

        if (!ExplicitlySet.Contains(nameof(Format)) && !string.IsNullOrEmpty(config.Format))
            Format = config.Format;

        if (!ExplicitlySet.Contains(nameof(OutputFile)) && !string.IsNullOrEmpty(config.OutputDirectory))
            OutputFile = config.OutputDirectory;

        if (!ExplicitlySet.Contains(nameof(FilterTags)) && config.Tags?.Include is { Count: > 0 })
            FilterTags = string.Join(",", config.Tags.Include);

        if (!ExplicitlySet.Contains(nameof(ExcludeTags)) && config.Tags?.Exclude is { Count: > 0 })
            ExcludeTags = string.Join(",", config.Tags.Exclude);

        if (!ExplicitlySet.Contains(nameof(Reporters)) && config.Reporters is { Count: > 0 })
            Reporters = string.Join(",", config.Reporters);

        // Coverage configuration
        if (!ExplicitlySet.Contains(nameof(Coverage)) && config.Coverage?.Enabled == true)
            Coverage = true;

        if (!ExplicitlySet.Contains(nameof(CoverageOutput)) && !string.IsNullOrEmpty(config.Coverage?.Output))
            CoverageOutput = config.Coverage.Output;

        if (!ExplicitlySet.Contains(nameof(CoverageFormat)) && !string.IsNullOrEmpty(config.Coverage?.Format))
            CoverageFormat = config.Coverage.Format;

        if (!ExplicitlySet.Contains(nameof(CoverageReportFormats)) && config.Coverage?.ReportFormats is { Count: > 0 })
            CoverageReportFormats = string.Join(",", config.Coverage.ReportFormats);
    }
}
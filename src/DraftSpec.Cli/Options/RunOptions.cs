using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'run' command.
/// Composes FilterOptions, CoverageOptions, and PartitionOptions.
/// </summary>
public class RunOptions
{
    /// <summary>
    /// Path to spec files or directory.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Output format for results.
    /// </summary>
    public OutputFormat Format { get; set; } = OutputFormat.Console;

    /// <summary>
    /// Output file path for non-console formats.
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Custom CSS URL for HTML output.
    /// </summary>
    public string? CssUrl { get; set; }

    /// <summary>
    /// Run specs in parallel.
    /// </summary>
    public bool Parallel { get; set; }

    /// <summary>
    /// Disable dotnet-script caching.
    /// </summary>
    public bool NoCache { get; set; }

    /// <summary>
    /// Stop after first failure.
    /// </summary>
    public bool Bail { get; set; }

    /// <summary>
    /// Disable pre-run statistics display.
    /// </summary>
    public bool NoStats { get; set; }

    /// <summary>
    /// Show statistics only without running specs.
    /// </summary>
    public bool StatsOnly { get; set; }

    /// <summary>
    /// Additional reporters (comma-separated).
    /// </summary>
    public string? Reporters { get; set; }

    /// <summary>
    /// Filter options for selecting which specs to run.
    /// </summary>
    public FilterOptions Filter { get; set; } = new();

    /// <summary>
    /// Coverage collection options.
    /// </summary>
    public CoverageOptions Coverage { get; set; } = new();

    /// <summary>
    /// Partitioning options for CI parallelism.
    /// </summary>
    public PartitionOptions Partition { get; set; } = new();

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
}

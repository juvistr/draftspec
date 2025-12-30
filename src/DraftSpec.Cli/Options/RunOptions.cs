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
}

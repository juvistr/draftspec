using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'watch' command.
/// Composes FilterOptions and includes watch-specific settings.
/// </summary>
public class WatchOptions
{
    /// <summary>
    /// Path to spec files or directory to watch.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Output format for results.
    /// </summary>
    public OutputFormat Format { get; set; } = OutputFormat.Console;

    /// <summary>
    /// Enable incremental watch mode (only re-run changed specs).
    /// When disabled, entire files are re-run on any change.
    /// </summary>
    public bool Incremental { get; set; }

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
    /// Filter options for selecting which specs to watch and run.
    /// </summary>
    public FilterOptions Filter { get; set; } = new();
}

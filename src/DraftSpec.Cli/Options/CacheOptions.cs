namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'cache' command.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// The cache subcommand: stats, clear.
    /// </summary>
    public string Subcommand { get; set; } = "stats";

    /// <summary>
    /// Directory to check for cache (defaults to current directory).
    /// </summary>
    public string Path { get; set; } = ".";
}

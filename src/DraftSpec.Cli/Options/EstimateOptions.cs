namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'estimate' command.
/// </summary>
public class EstimateOptions
{
    /// <summary>
    /// Path to the project root directory.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Percentile to use for estimation (1-99). Default is 50 (median).
    /// </summary>
    public int Percentile { get; set; } = 50;

    /// <summary>
    /// Output the estimate in seconds (machine-readable) instead of formatted time.
    /// </summary>
    public bool OutputSeconds { get; set; }
}

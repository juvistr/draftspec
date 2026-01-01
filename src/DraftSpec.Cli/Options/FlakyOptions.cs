namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'flaky' command.
/// </summary>
public class FlakyOptions
{
    /// <summary>
    /// Path to the project root directory.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Minimum number of status changes to be considered flaky.
    /// </summary>
    public int MinStatusChanges { get; set; } = 2;

    /// <summary>
    /// Number of recent runs to analyze.
    /// </summary>
    public int WindowSize { get; set; } = 10;

    /// <summary>
    /// Clear history for a specific spec ID.
    /// </summary>
    public string? Clear { get; set; }
}

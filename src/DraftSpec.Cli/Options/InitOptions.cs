namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'init' command.
/// </summary>
public class InitOptions
{
    /// <summary>
    /// Directory to initialize with spec_helper.csx and omnisharp.json.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Overwrite existing files if they exist.
    /// </summary>
    public bool Force { get; set; }
}

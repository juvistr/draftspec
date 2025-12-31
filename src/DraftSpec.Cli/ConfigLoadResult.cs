using System.Text.Json;

namespace DraftSpec.Cli.Configuration;

/// <summary>
/// Result of loading a configuration file.
/// </summary>
public record ConfigLoadResult(
    DraftSpecProjectConfig? Config,
    string? Error,
    string? FilePath)
{
    /// <summary>
    /// Whether the config was loaded successfully.
    /// </summary>
    public bool Success => Error == null && Config != null;

    /// <summary>
    /// Whether a config file was found (even if it failed to parse).
    /// </summary>
    public bool Found => FilePath != null;
}

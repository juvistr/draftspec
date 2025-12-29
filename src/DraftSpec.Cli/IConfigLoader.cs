using DraftSpec.Cli.Configuration;

namespace DraftSpec.Cli;

/// <summary>
/// Interface for loading project configuration.
/// </summary>
public interface IConfigLoader
{
    /// <summary>
    /// Load configuration from draftspec.json in the specified path.
    /// </summary>
    /// <param name="path">Path to search for configuration (defaults to current directory)</param>
    /// <returns>Configuration result with config or error</returns>
    ConfigLoadResult Load(string? path = null);
}

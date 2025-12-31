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

/// <summary>
/// Loads project configuration from draftspec.json files.
/// </summary>
public class ConfigLoader : IConfigLoader
{
    /// <summary>
    /// The configuration file name to search for.
    /// </summary>
    public const string ConfigFileName = "draftspec.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IEnvironment _environment;

    public ConfigLoader(IEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Load configuration from the specified path or current directory.
    /// </summary>
    /// <param name="path">Path to search in. Defaults to current directory.</param>
    /// <returns>The loaded configuration, or null if no config file exists.</returns>
    public ConfigLoadResult Load(string? path = null)
    {
        var searchDir = path ?? _environment.CurrentDirectory;
        var configPath = FindConfigFile(searchDir);

        if (configPath == null)
            return new ConfigLoadResult(null, null, null);

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<DraftSpecProjectConfig>(json, JsonOptions);

            if (config == null)
                return new ConfigLoadResult(null, "Configuration file is empty or invalid", configPath);

            // Validate the configuration
            var errors = config.Validate();
            if (errors.Count > 0)
                return new ConfigLoadResult(null, $"Invalid configuration: {string.Join(", ", errors)}", configPath);

            return new ConfigLoadResult(config, null, configPath);
        }
        catch (JsonException ex)
        {
            return new ConfigLoadResult(null, $"Error parsing {ConfigFileName}: {ex.Message}", configPath);
        }
        catch (IOException ex)
        {
            return new ConfigLoadResult(null, $"Error reading {ConfigFileName}: {ex.Message}", configPath);
        }
    }

    /// <summary>
    /// Find the configuration file in the specified directory.
    /// </summary>
    /// <param name="startDirectory">Directory to search in.</param>
    /// <returns>Full path to the config file, or null if not found.</returns>
    public static string? FindConfigFile(string startDirectory)
    {
        var dir = Path.GetFullPath(startDirectory);

        // If it's a file path, get its directory
        if (File.Exists(dir))
            dir = Path.GetDirectoryName(dir) ?? dir;

        var configPath = Path.Combine(dir, ConfigFileName);
        return File.Exists(configPath) ? configPath : null;
    }
}

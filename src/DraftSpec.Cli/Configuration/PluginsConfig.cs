using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Configuration;

/// <summary>
/// Configuration for plugin loading and security.
/// </summary>
public class PluginsConfig
{
    /// <summary>
    /// When true, only load plugins that are signed with a trusted public key.
    /// Default: false (allow all plugins).
    /// </summary>
    [JsonPropertyName("requireSignedPlugins")]
    public bool RequireSignedPlugins { get; set; } = false;

    /// <summary>
    /// List of trusted public key tokens (hex strings, e.g., "b77a5c561934e089").
    /// Plugins must be signed with one of these keys when RequireSignedPlugins is true.
    /// </summary>
    [JsonPropertyName("trustedPublicKeyTokens")]
    public List<string>? TrustedPublicKeyTokens { get; set; }
}

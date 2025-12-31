using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Configuration;

/// <summary>
/// Coverage threshold configuration.
/// </summary>
public class ThresholdsConfig
{
    /// <summary>
    /// Minimum line coverage percentage (0-100).
    /// </summary>
    [JsonPropertyName("line")]
    public double? Line { get; set; }

    /// <summary>
    /// Minimum branch coverage percentage (0-100).
    /// </summary>
    [JsonPropertyName("branch")]
    public double? Branch { get; set; }
}

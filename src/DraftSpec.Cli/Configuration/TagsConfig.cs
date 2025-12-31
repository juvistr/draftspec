using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Configuration;

/// <summary>
/// Tag filtering configuration for including/excluding specs by tag.
/// </summary>
public class TagsConfig
{
    /// <summary>
    /// Tags to include. Only specs with any of these tags will run.
    /// </summary>
    [JsonPropertyName("include")]
    public List<string>? Include { get; set; }

    /// <summary>
    /// Tags to exclude. Specs with any of these tags will be skipped.
    /// </summary>
    [JsonPropertyName("exclude")]
    public List<string>? Exclude { get; set; }
}
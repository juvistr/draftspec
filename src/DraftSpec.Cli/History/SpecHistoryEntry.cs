using System.Text.Json.Serialization;

namespace DraftSpec.Cli.History;

/// <summary>
/// History entry for a single spec.
/// </summary>
public sealed class SpecHistoryEntry
{
    /// <summary>
    /// Human-readable display name (e.g., "TodoService > CreateAsync > creates todo").
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Recent run history, most recent first.
    /// Limited to MaxRuns entries.
    /// </summary>
    [JsonPropertyName("runs")]
    public List<SpecRun> Runs { get; set; } = new();

    /// <summary>
    /// Maximum number of runs to retain per spec.
    /// </summary>
    public const int MaxRuns = 50;
}

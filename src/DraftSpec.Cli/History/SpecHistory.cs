using System.Text.Json.Serialization;

namespace DraftSpec.Cli.History;

/// <summary>
/// Root container for spec execution history.
/// Stored in .draftspec/history.json.
/// </summary>
public sealed class SpecHistory
{
    /// <summary>
    /// Schema version for forward compatibility.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the history file was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Spec entries keyed by stable spec ID.
    /// </summary>
    [JsonPropertyName("specs")]
    public Dictionary<string, SpecHistoryEntry> Specs { get; set; } = new();

    /// <summary>
    /// Returns an empty history instance.
    /// </summary>
    public static SpecHistory Empty => new() { UpdatedAt = DateTime.UtcNow };
}

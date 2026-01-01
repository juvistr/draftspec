using System.Text.Json.Serialization;

namespace DraftSpec.Cli.History;

/// <summary>
/// Record of a single spec execution.
/// </summary>
public sealed class SpecRun
{
    /// <summary>
    /// When the spec was executed.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Execution result: "passed", "failed", "pending", or "skipped".
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public double DurationMs { get; set; }

    /// <summary>
    /// Error message if the spec failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; set; }
}

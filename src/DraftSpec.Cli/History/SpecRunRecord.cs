namespace DraftSpec.Cli.History;

/// <summary>
/// Record for submitting spec results to the history service.
/// </summary>
public sealed class SpecRunRecord
{
    /// <summary>
    /// Stable spec ID.
    /// </summary>
    public required string SpecId { get; init; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Execution result status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public required double DurationMs { get; init; }

    /// <summary>
    /// Error message if the spec failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

namespace DraftSpec.Cli.History;

/// <summary>
/// Result of flaky test detection for a single spec.
/// </summary>
public sealed class FlakySpec
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
    /// Number of status transitions (passed<->failed) in the analysis window.
    /// </summary>
    public required int StatusChanges { get; init; }

    /// <summary>
    /// Total runs analyzed.
    /// </summary>
    public required int TotalRuns { get; init; }

    /// <summary>
    /// Ratio of passed runs to total runs (0.0 to 1.0).
    /// </summary>
    public required double PassRate { get; init; }

    /// <summary>
    /// Timestamp of the most recent run.
    /// </summary>
    public DateTime? LastSeen { get; init; }

    /// <summary>
    /// Flakiness severity based on status changes.
    /// </summary>
    public string Severity => StatusChanges switch
    {
        >= 4 => "HIGH",
        >= 2 => "MEDIUM",
        _ => "LOW"
    };
}

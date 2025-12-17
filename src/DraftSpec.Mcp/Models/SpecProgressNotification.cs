namespace DraftSpec.Mcp.Models;

/// <summary>
/// Progress notification sent during spec execution.
/// </summary>
public record SpecProgressNotification
{
    /// <summary>
    /// The type of progress event: "start", "progress", or "complete".
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The full description of the spec (for "progress" type).
    /// </summary>
    public string? Spec { get; init; }

    /// <summary>
    /// The status of the completed spec: "passed", "failed", "pending", "skipped".
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Number of specs completed so far.
    /// </summary>
    public int Completed { get; init; }

    /// <summary>
    /// Total number of specs to run.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Number of passed specs (for "complete" type).
    /// </summary>
    public int? Passed { get; init; }

    /// <summary>
    /// Number of failed specs (for "complete" type).
    /// </summary>
    public int? Failed { get; init; }

    /// <summary>
    /// Number of pending specs (for "complete" type).
    /// </summary>
    public int? Pending { get; init; }

    /// <summary>
    /// Number of skipped specs (for "complete" type).
    /// </summary>
    public int? Skipped { get; init; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double ProgressPercent => Total > 0 ? (double)Completed / Total * 100 : 0;
}

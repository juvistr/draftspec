namespace DraftSpec.Mcp.Models;

/// <summary>
/// Result of batch spec execution.
/// </summary>
public class BatchSpecResult
{
    /// <summary>
    /// Whether all specs passed.
    /// </summary>
    public bool AllPassed { get; init; }

    /// <summary>
    /// Total number of specs executed.
    /// </summary>
    public int TotalSpecs { get; init; }

    /// <summary>
    /// Number of specs that passed.
    /// </summary>
    public int PassedSpecs { get; init; }

    /// <summary>
    /// Number of specs that failed.
    /// </summary>
    public int FailedSpecs { get; init; }

    /// <summary>
    /// Total execution time in milliseconds.
    /// </summary>
    public double TotalDurationMs { get; init; }

    /// <summary>
    /// Individual results for each spec.
    /// </summary>
    public List<NamedSpecResult> Results { get; init; } = [];

    /// <summary>
    /// Error message if the batch failed before execution (e.g., validation error).
    /// </summary>
    public string? Error { get; init; }
}

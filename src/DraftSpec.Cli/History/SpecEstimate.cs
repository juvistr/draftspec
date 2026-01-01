namespace DraftSpec.Cli.History;

/// <summary>
/// Runtime estimate for an individual spec.
/// </summary>
public class SpecEstimate
{
    /// <summary>
    /// Unique identifier for the spec.
    /// </summary>
    public required string SpecId { get; init; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Estimated runtime at the requested percentile in milliseconds.
    /// </summary>
    public double EstimateMs { get; init; }

    /// <summary>
    /// Number of historical runs for this spec.
    /// </summary>
    public int RunCount { get; init; }
}

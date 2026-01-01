namespace DraftSpec.Cli.History;

/// <summary>
/// Aggregated runtime estimate for a spec suite.
/// </summary>
public class RuntimeEstimate
{
    /// <summary>
    /// Median (P50) total runtime in milliseconds.
    /// </summary>
    public double P50Ms { get; init; }

    /// <summary>
    /// 95th percentile total runtime in milliseconds.
    /// </summary>
    public double P95Ms { get; init; }

    /// <summary>
    /// Maximum observed total runtime in milliseconds.
    /// </summary>
    public double MaxMs { get; init; }

    /// <summary>
    /// Total estimated runtime at the requested percentile in milliseconds.
    /// </summary>
    public double TotalEstimateMs { get; init; }

    /// <summary>
    /// The percentile used for TotalEstimateMs.
    /// </summary>
    public int Percentile { get; init; }

    /// <summary>
    /// Number of historical runs analyzed.
    /// </summary>
    public int SampleSize { get; init; }

    /// <summary>
    /// Number of unique specs with history.
    /// </summary>
    public int SpecCount { get; init; }

    /// <summary>
    /// Top slowest specs ordered by estimated duration.
    /// </summary>
    public IReadOnlyList<SpecEstimate> SlowestSpecs { get; init; } = [];
}

namespace DraftSpec.Cli.History;

/// <summary>
/// Calculates runtime estimates based on historical execution data.
/// </summary>
public interface IRuntimeEstimator
{
    /// <summary>
    /// Calculate runtime estimates from spec history.
    /// </summary>
    RuntimeEstimate Calculate(SpecHistory history, int percentile = 50);

    /// <summary>
    /// Calculate a percentile value from a list of durations.
    /// </summary>
    double CalculatePercentile(IReadOnlyList<double> values, int percentile);
}

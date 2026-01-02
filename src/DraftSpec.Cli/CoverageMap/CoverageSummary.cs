namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Summary statistics for coverage map results.
/// </summary>
public sealed class CoverageSummary
{
    /// <summary>
    /// Total number of public methods analyzed.
    /// </summary>
    public int TotalMethods { get; init; }

    /// <summary>
    /// Methods with HIGH confidence coverage.
    /// </summary>
    public int HighConfidence { get; init; }

    /// <summary>
    /// Methods with MEDIUM confidence coverage.
    /// </summary>
    public int MediumConfidence { get; init; }

    /// <summary>
    /// Methods with LOW confidence coverage.
    /// </summary>
    public int LowConfidence { get; init; }

    /// <summary>
    /// Methods with no detected coverage.
    /// </summary>
    public int Uncovered { get; init; }

    /// <summary>
    /// Percentage of methods with any coverage (non-zero confidence).
    /// </summary>
    public double CoveragePercentage => TotalMethods > 0
        ? (double)(TotalMethods - Uncovered) / TotalMethods * 100
        : 0;
}

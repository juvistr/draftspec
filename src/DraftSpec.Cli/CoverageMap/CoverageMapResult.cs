namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Complete result of coverage map analysis.
/// </summary>
public sealed class CoverageMapResult
{
    /// <summary>
    /// Coverage information for all analyzed methods.
    /// </summary>
    public IReadOnlyList<MethodCoverage> AllMethods { get; init; } = [];

    /// <summary>
    /// Methods with any coverage (confidence > None).
    /// </summary>
    public IReadOnlyList<MethodCoverage> CoveredMethods =>
        AllMethods.Where(m => m.Confidence != CoverageConfidence.None).ToList();

    /// <summary>
    /// Methods with no detected coverage.
    /// </summary>
    public IReadOnlyList<MethodCoverage> UncoveredMethods =>
        AllMethods.Where(m => m.Confidence == CoverageConfidence.None).ToList();

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public CoverageSummary Summary { get; init; } = new();

    /// <summary>
    /// Path to the analyzed source files.
    /// </summary>
    public string? SourcePath { get; init; }

    /// <summary>
    /// Path to the spec files.
    /// </summary>
    public string? SpecPath { get; init; }
}

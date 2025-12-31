namespace DraftSpec.Coverage;

/// <summary>
/// Coverage data for a single spec execution.
/// Records which lines were covered during the spec's execution.
/// </summary>
public sealed record SpecCoverageData
{
    /// <summary>
    /// Unique identifier for the spec (full description path).
    /// </summary>
    public required string SpecId { get; init; }

    /// <summary>
    /// Files covered during this spec's execution.
    /// Key: file path, Value: coverage data for that file.
    /// </summary>
    public required IReadOnlyDictionary<string, CoveredFile> FilesCovered { get; init; }

    /// <summary>
    /// Summary statistics for this spec's coverage.
    /// </summary>
    public required SpecCoverageSummary Summary { get; init; }
}

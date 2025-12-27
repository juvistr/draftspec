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

/// <summary>
/// Coverage data for a single file within a spec's execution.
/// </summary>
public sealed record CoveredFile
{
    /// <summary>
    /// Full path to the source file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Lines that were executed during this spec.
    /// Key: line number, Value: hit count delta.
    /// </summary>
    public required IReadOnlyDictionary<int, int> LineHits { get; init; }

    /// <summary>
    /// Branches covered during this spec (if branch coverage enabled).
    /// Key: line number, Value: branches covered/total.
    /// </summary>
    public IReadOnlyDictionary<int, BranchCoverage>? BranchHits { get; init; }

    /// <summary>
    /// Number of lines covered in this file by this spec.
    /// </summary>
    public int LinesCovered => LineHits.Count(kv => kv.Value > 0);
}

/// <summary>
/// Branch coverage data for a specific line.
/// </summary>
public sealed record BranchCoverage
{
    /// <summary>
    /// Number of branches covered.
    /// </summary>
    public required int Covered { get; init; }

    /// <summary>
    /// Total number of branches at this point.
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// Branch coverage percentage (0-100).
    /// </summary>
    public double Percent => Total > 0 ? (double)Covered / Total * 100 : 0;
}

/// <summary>
/// Summary statistics for a spec's coverage contribution.
/// </summary>
public sealed record SpecCoverageSummary
{
    /// <summary>
    /// Total lines covered by this spec.
    /// </summary>
    public required int LinesCovered { get; init; }

    /// <summary>
    /// Total branches covered by this spec.
    /// </summary>
    public int BranchesCovered { get; init; }

    /// <summary>
    /// Number of files touched by this spec.
    /// </summary>
    public required int FilesTouched { get; init; }
}

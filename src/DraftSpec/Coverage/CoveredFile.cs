namespace DraftSpec.Coverage;

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

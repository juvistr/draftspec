namespace DraftSpec.Coverage;

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
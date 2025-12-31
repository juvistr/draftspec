namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Coverage data for a single line of code.
/// </summary>
public class LineCoverage
{
    /// <summary>
    /// 1-based line number.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Number of times this line was executed.
    /// </summary>
    public int Hits { get; set; }

    /// <summary>
    /// Whether this line is a branch point.
    /// </summary>
    public bool IsBranchPoint { get; set; }

    /// <summary>
    /// Number of branches covered (if branch point).
    /// </summary>
    public int? BranchesCovered { get; set; }

    /// <summary>
    /// Total branches at this point (if branch point).
    /// </summary>
    public int? BranchesTotal { get; set; }

    /// <summary>
    /// Coverage status for this line.
    /// </summary>
    public CoverageStatus Status => Hits > 0
        ? (IsBranchPoint && BranchesCovered < BranchesTotal ? CoverageStatus.Partial : CoverageStatus.Covered)
        : CoverageStatus.Uncovered;
}

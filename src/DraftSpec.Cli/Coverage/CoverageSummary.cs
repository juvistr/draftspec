namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Summary of coverage statistics.
/// </summary>
public class CoverageSummary
{
    /// <summary>
    /// Total number of coverable lines.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Number of lines that were executed.
    /// </summary>
    public int CoveredLines { get; set; }

    /// <summary>
    /// Total number of branches.
    /// </summary>
    public int TotalBranches { get; set; }

    /// <summary>
    /// Number of branches that were taken.
    /// </summary>
    public int CoveredBranches { get; set; }

    /// <summary>
    /// Line coverage percentage (0-100).
    /// </summary>
    public double LinePercent => TotalLines > 0 ? (double)CoveredLines / TotalLines * 100 : 0;

    /// <summary>
    /// Branch coverage percentage (0-100).
    /// </summary>
    public double BranchPercent => TotalBranches > 0 ? (double)CoveredBranches / TotalBranches * 100 : 0;
}
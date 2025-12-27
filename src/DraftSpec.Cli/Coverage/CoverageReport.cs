namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Represents a code coverage report.
/// </summary>
public class CoverageReport
{
    /// <summary>
    /// When the coverage was collected.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source directory for relative paths.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Overall coverage summary.
    /// </summary>
    public CoverageSummary Summary { get; set; } = new();

    /// <summary>
    /// Per-file coverage data.
    /// </summary>
    public List<FileCoverage> Files { get; set; } = [];
}

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

/// <summary>
/// Coverage data for a single source file.
/// </summary>
public class FileCoverage
{
    /// <summary>
    /// Full or relative path to the source file.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Package or namespace name (from Cobertura).
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Total coverable lines in this file.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Lines covered in this file.
    /// </summary>
    public int CoveredLines { get; set; }

    /// <summary>
    /// Total branches in this file.
    /// </summary>
    public int TotalBranches { get; set; }

    /// <summary>
    /// Branches covered in this file.
    /// </summary>
    public int CoveredBranches { get; set; }

    /// <summary>
    /// Line coverage percentage for this file.
    /// </summary>
    public double LinePercent => TotalLines > 0 ? (double)CoveredLines / TotalLines * 100 : 0;

    /// <summary>
    /// Per-line coverage data.
    /// </summary>
    public List<LineCoverage> Lines { get; set; } = [];
}

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

/// <summary>
/// Coverage status for a line of code.
/// </summary>
public enum CoverageStatus
{
    /// <summary>
    /// Line was executed.
    /// </summary>
    Covered,

    /// <summary>
    /// Line was not executed.
    /// </summary>
    Uncovered,

    /// <summary>
    /// Line was executed but not all branches taken.
    /// </summary>
    Partial
}

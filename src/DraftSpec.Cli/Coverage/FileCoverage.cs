namespace DraftSpec.Cli.Coverage;

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

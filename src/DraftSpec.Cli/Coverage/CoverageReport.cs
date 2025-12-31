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

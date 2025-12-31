namespace DraftSpec.Coverage;

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
namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Result of coverage threshold check.
/// </summary>
public class CoverageThresholdResult
{
    /// <summary>
    /// Whether all thresholds were met.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// Actual line coverage percentage.
    /// </summary>
    public double ActualLinePercent { get; init; }

    /// <summary>
    /// Required line coverage percentage (if configured).
    /// </summary>
    public double? RequiredLinePercent { get; init; }

    /// <summary>
    /// Actual branch coverage percentage.
    /// </summary>
    public double ActualBranchPercent { get; init; }

    /// <summary>
    /// Required branch coverage percentage (if configured).
    /// </summary>
    public double? RequiredBranchPercent { get; init; }

    /// <summary>
    /// List of threshold failures.
    /// </summary>
    public List<string> Failures { get; init; } = [];

    /// <summary>
    /// Combined failure message.
    /// </summary>
    public string FailureMessage => string.Join(Environment.NewLine, Failures);
}

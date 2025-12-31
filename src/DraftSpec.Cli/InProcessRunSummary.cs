namespace DraftSpec.Cli;

/// <summary>
/// Summary of running multiple spec files.
/// </summary>
public record InProcessRunSummary(
    IReadOnlyList<InProcessRunResult> Results,
    TimeSpan TotalDuration)
{
    public bool Success => Results.All(r => r.Success);
    public int TotalSpecs => Results.Sum(r => r.Report.Summary.Total);
    public int Passed => Results.Sum(r => r.Report.Summary.Passed);
    public int Failed => Results.Sum(r => r.Report.Summary.Failed);
    public int Pending => Results.Sum(r => r.Report.Summary.Pending);
    public int Skipped => Results.Sum(r => r.Report.Summary.Skipped);
}
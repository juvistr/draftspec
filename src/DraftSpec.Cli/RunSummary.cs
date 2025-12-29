namespace DraftSpec.Cli;

/// <summary>
/// Summary of running multiple spec files (legacy format for ConsolePresenter).
/// </summary>
public record RunSummary(
    IReadOnlyList<SpecRunResult> Results,
    TimeSpan TotalDuration)
{
    public bool Success => Results.All(r => r.Success);
    public int TotalSpecs => Results.Count;
    public int Passed => Results.Count(r => r.Success);
    public int Failed => Results.Count(r => !r.Success);
}

namespace DraftSpec.Formatters.Abstractions;

/// <summary>
/// Summary statistics for the spec run.
/// </summary>
public class SpecSummary
{
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
    public int Skipped { get; set; }
    public double DurationMs { get; set; }

    public bool Success => Failed == 0;
}

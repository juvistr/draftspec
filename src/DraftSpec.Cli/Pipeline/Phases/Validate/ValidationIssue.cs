namespace DraftSpec.Cli.Pipeline.Phases.Validate;

/// <summary>
/// A single validation issue (error or warning).
/// </summary>
public class ValidationIssue
{
    public int? LineNumber { get; set; }
    public string Message { get; set; } = "";
}

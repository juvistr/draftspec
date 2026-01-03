namespace DraftSpec.Cli.Pipeline.Phases.Validate;

/// <summary>
/// Result of validating a single spec file.
/// </summary>
public class FileValidationResult
{
    public string FilePath { get; set; } = "";
    public int SpecCount { get; set; }
    public List<ValidationIssue> Errors { get; } = [];
    public List<ValidationIssue> Warnings { get; } = [];
}

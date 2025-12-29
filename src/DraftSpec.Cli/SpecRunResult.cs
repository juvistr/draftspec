namespace DraftSpec.Cli;

/// <summary>
/// Result of running a single spec file (legacy format for ConsolePresenter).
/// </summary>
public record SpecRunResult(
    string SpecFile,
    string Output,
    string Error,
    int ExitCode,
    TimeSpan Duration)
{
    public bool Success => ExitCode == 0;
}

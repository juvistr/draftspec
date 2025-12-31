using DraftSpec.Formatters;

namespace DraftSpec.Cli;

/// <summary>
/// Result of running specs from a single file.
/// </summary>
public record InProcessRunResult(
    string SpecFile,
    SpecReport Report,
    TimeSpan Duration,
    Exception? Error = null)
{
    public bool Success => Error == null && Report.Summary.Failed == 0;
}

namespace DraftSpec.Cli;

/// <summary>
/// Result of a build operation.
/// </summary>
public record BuildResult(bool Success, string Output, string Error, bool Skipped = false);

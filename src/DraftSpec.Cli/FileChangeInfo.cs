namespace DraftSpec.Cli;

/// <summary>
/// Information about a file change event.
/// </summary>
/// <param name="FilePath">The changed file path, or null if multiple files changed</param>
/// <param name="IsSpecFile">True if a single spec file changed, false if source or multiple files changed</param>
public record FileChangeInfo(string? FilePath, bool IsSpecFile);

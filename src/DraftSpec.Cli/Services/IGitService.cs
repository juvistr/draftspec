namespace DraftSpec.Cli.Services;

/// <summary>
/// Abstraction for git operations, enabling deterministic testing.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Gets a list of files changed according to the specified reference.
    /// </summary>
    /// <param name="reference">
    /// Can be:
    /// - "staged" - only staged changes (git diff --cached)
    /// - A commit ref like "HEAD~1" or "main" - changes since that commit
    /// - A file path - reads the file as a list of file paths
    /// </param>
    /// <param name="workingDirectory">The working directory for git commands.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of absolute file paths that changed.</returns>
    Task<IReadOnlyList<string>> GetChangedFilesAsync(
        string reference,
        string workingDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified directory is inside a git repository.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the directory is inside a git repository.</returns>
    Task<bool> IsGitRepositoryAsync(
        string directory,
        CancellationToken cancellationToken = default);
}

using System.Diagnostics;

namespace DraftSpec.Cli;

/// <summary>
/// Helper for getting changed files from git.
/// </summary>
public static class GitHelper
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
    public static async Task<IReadOnlyList<string>> GetChangedFilesAsync(
        string reference,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        // Check if reference is a file path
        var fullReference = Path.GetFullPath(reference, workingDirectory);
        if (File.Exists(fullReference))
        {
            var lines = await File.ReadAllLinesAsync(fullReference, cancellationToken);
            return lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => Path.GetFullPath(line.Trim(), workingDirectory))
                .ToList();
        }

        // Build git diff arguments
        var args = reference.Equals("staged", StringComparison.OrdinalIgnoreCase)
            ? "diff --cached --name-only"
            : $"diff {reference} --name-only";

        var output = await RunGitAsync(args, workingDirectory, cancellationToken);

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(file => Path.GetFullPath(file.Trim(), workingDirectory))
            .ToList();
    }

    /// <summary>
    /// Checks if the current directory is inside a git repository.
    /// </summary>
    public static async Task<bool> IsGitRepositoryAsync(
        string directory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await RunGitAsync("rev-parse --is-inside-work-tree", directory, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> RunGitAsync(
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}

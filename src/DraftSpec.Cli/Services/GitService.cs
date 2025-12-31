using System.Diagnostics;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Implementation of git operations using the git CLI.
/// </summary>
public class GitService : IGitService
{
    private readonly IFileSystem _fileSystem;

    public GitService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetChangedFilesAsync(
        string reference,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        // Check if reference is a file path
        var fullReference = Path.GetFullPath(reference, workingDirectory);
        if (_fileSystem.FileExists(fullReference))
        {
            var content = await _fileSystem.ReadAllTextAsync(fullReference, cancellationToken);
            return content
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
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

    /// <inheritdoc />
    public async Task<bool> IsGitRepositoryAsync(
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

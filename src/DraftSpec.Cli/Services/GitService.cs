using System.Diagnostics;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Implementation of git operations using the git CLI.
/// </summary>
public class GitService : IGitService
{
    private readonly IFileSystem _fileSystem;
    private readonly TimeSpan _commandTimeout;

    /// <summary>
    /// Default timeout for git commands (30 seconds).
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public GitService(IFileSystem fileSystem)
        : this(fileSystem, DefaultTimeout)
    {
    }

    public GitService(IFileSystem fileSystem, TimeSpan commandTimeout)
    {
        _fileSystem = fileSystem;
        _commandTimeout = commandTimeout;
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
            var content = await _fileSystem.ReadAllTextAsync(fullReference, cancellationToken).ConfigureAwait(false);
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

    private async Task<string> RunGitAsync(
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
                RedirectStandardInput = true, // Prevent git from waiting for input
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        // Disable all forms of credential prompting
        process.StartInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
        process.StartInfo.EnvironmentVariables["GIT_ASKPASS"] = "";
        process.StartInfo.EnvironmentVariables["GCM_INTERACTIVE"] = "never";

        process.Start();

        // Close stdin immediately to signal no input will be provided
        process.StandardInput.Close();

        // Read stdout and stderr in parallel to avoid deadlock when buffers fill
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        // Use a timeout to prevent hanging forever
        using var timeoutCts = new CancellationTokenSource(_commandTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await Task.WhenAll(outputTask, errorTask).WaitAsync(linkedCts.Token).ConfigureAwait(false);
            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            process.Kill();
            throw new InvalidOperationException($"Git command timed out after {_commandTimeout.TotalSeconds} seconds");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}

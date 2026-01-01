using DraftSpec.Cli;

namespace DraftSpec.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Fluent builder for creating test git repositories.
/// </summary>
public class GitRepositoryBuilder
{
    private readonly string _directory;
    private readonly List<(string Path, string Content)> _initialFiles = [];
    private readonly List<string> _commitMessages = [];
    private readonly List<(string Path, string Content)> _stagedChanges = [];
    private readonly List<(string Path, string Content)> _unstagedChanges = [];

    public GitRepositoryBuilder(string directory)
    {
        _directory = directory;
    }

    /// <summary>
    /// Adds a file to the repository.
    /// If called before first commit, file is included in initial commit.
    /// </summary>
    public GitRepositoryBuilder WithFile(string relativePath, string content)
    {
        _initialFiles.Add((relativePath, content));
        return this;
    }

    /// <summary>
    /// Creates a commit with all pending files.
    /// </summary>
    public GitRepositoryBuilder WithCommit(string message)
    {
        _commitMessages.Add(message);
        return this;
    }

    /// <summary>
    /// Stages a file change (for testing --affected-by staged).
    /// </summary>
    public GitRepositoryBuilder WithStagedChange(string relativePath, string content)
    {
        _stagedChanges.Add((relativePath, content));
        return this;
    }

    /// <summary>
    /// Adds an unstaged modification (for testing --affected-by HEAD).
    /// </summary>
    public GitRepositoryBuilder WithUnstagedChange(string relativePath, string content)
    {
        _unstagedChanges.Add((relativePath, content));
        return this;
    }

    /// <summary>
    /// Builds the git repository and returns the path.
    /// </summary>
    public string Build()
    {
        Directory.CreateDirectory(_directory);

        // Initialize git repo
        RunGit("init");
        RunGit("config", "user.email", "test@example.com");
        RunGit("config", "user.name", "Test User");

        // Write initial files
        foreach (var (path, content) in _initialFiles)
        {
            var fullPath = Path.Combine(_directory, path);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, content);
        }

        // Stage and commit
        if (_initialFiles.Count > 0 || _commitMessages.Count > 0)
        {
            RunGit("add", ".");

            var commitMessage = _commitMessages.Count > 0 ? _commitMessages[0] : "Initial commit";
            RunGit("commit", "-m", commitMessage);
        }

        // Handle additional commits if specified
        for (var i = 1; i < _commitMessages.Count; i++)
        {
            // Create a dummy file change for each additional commit
            var dummyFile = Path.Combine(_directory, $".commit{i}");
            File.WriteAllText(dummyFile, $"Commit {i}");
            RunGit("add", ".");
            RunGit("commit", "-m", _commitMessages[i]);
        }

        // Apply staged changes
        foreach (var (path, content) in _stagedChanges)
        {
            var fullPath = Path.Combine(_directory, path);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, content);
            RunGit("add", path);
        }

        // Apply unstaged changes
        foreach (var (path, content) in _unstagedChanges)
        {
            var fullPath = Path.Combine(_directory, path);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, content);
        }

        return _directory;
    }

    private ProcessResult RunGit(params string[] args)
    {
        var result = ProcessHelper.Run("git", args, _directory);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Git command failed: git {string.Join(" ", args)}\n{result.Error}");
        }
        return result;
    }
}

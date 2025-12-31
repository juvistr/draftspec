using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IGitService for testing.
/// </summary>
public class MockGitService : IGitService
{
    private IReadOnlyList<string> _changedFiles = [];
    private bool _isGitRepository = true;
    private Exception? _throwOnGetChangedFiles;

    public List<(string Reference, string WorkingDirectory)> GetChangedFilesCalls { get; } = [];
    public List<string> IsGitRepositoryCalls { get; } = [];

    /// <summary>
    /// Configure the list of files returned by GetChangedFilesAsync.
    /// </summary>
    public MockGitService WithChangedFiles(params string[] files)
    {
        _changedFiles = files;
        return this;
    }

    /// <summary>
    /// Configure the service to throw on GetChangedFilesAsync.
    /// </summary>
    public MockGitService ThrowsOnGetChangedFiles(Exception exception)
    {
        _throwOnGetChangedFiles = exception;
        return this;
    }

    /// <summary>
    /// Configure IsGitRepositoryAsync to return false.
    /// </summary>
    public MockGitService NotAGitRepository()
    {
        _isGitRepository = false;
        return this;
    }

    public Task<IReadOnlyList<string>> GetChangedFilesAsync(
        string reference,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        GetChangedFilesCalls.Add((reference, workingDirectory));

        if (_throwOnGetChangedFiles is not null)
            throw _throwOnGetChangedFiles;

        return Task.FromResult(_changedFiles);
    }

    public Task<bool> IsGitRepositoryAsync(
        string directory,
        CancellationToken cancellationToken = default)
    {
        IsGitRepositoryCalls.Add(directory);
        return Task.FromResult(_isGitRepository);
    }
}

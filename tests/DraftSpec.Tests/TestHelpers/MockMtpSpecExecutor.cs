using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.TestHelpers;

/// <summary>
/// Mock implementation of IMtpSpecExecutor for testing.
/// </summary>
internal class MockMtpSpecExecutor : IMtpSpecExecutor
{
    private readonly Dictionary<string, ExecutionResult> _fileResults = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ExecutionResult> _idResults = [];

    public List<string> ExecuteFileAsyncCalls { get; } = [];
    public List<(string Path, HashSet<string>? Ids)> ExecuteFileAsyncWithIdsCalls { get; } = [];
    public List<IEnumerable<string>> ExecuteByIdsAsyncCalls { get; } = [];

    /// <summary>
    /// Configures the mock to return a specific result for a file path.
    /// </summary>
    public MockMtpSpecExecutor WithFileResult(string path, ExecutionResult result)
    {
        _fileResults[path] = result;
        return this;
    }

    /// <summary>
    /// Configures the mock to return specific results from ExecuteByIdsAsync.
    /// </summary>
    public MockMtpSpecExecutor WithIdResults(params ExecutionResult[] results)
    {
        _idResults.AddRange(results);
        return this;
    }

    public Task<ExecutionResult> ExecuteFileAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default)
    {
        ExecuteFileAsyncCalls.Add(csxFilePath);

        if (_fileResults.TryGetValue(csxFilePath, out var result))
        {
            return Task.FromResult(result);
        }

        return Task.FromResult(new ExecutionResult(
            Path.GetFileName(csxFilePath),
            csxFilePath,
            []));
    }

    public Task<ExecutionResult> ExecuteFileAsync(
        string csxFilePath,
        HashSet<string>? requestedIds,
        CancellationToken cancellationToken = default)
    {
        ExecuteFileAsyncWithIdsCalls.Add((csxFilePath, requestedIds));

        if (_fileResults.TryGetValue(csxFilePath, out var result))
        {
            return Task.FromResult(result);
        }

        return Task.FromResult(new ExecutionResult(
            Path.GetFileName(csxFilePath),
            csxFilePath,
            []));
    }

    public Task<IReadOnlyList<ExecutionResult>> ExecuteByIdsAsync(
        IEnumerable<string> requestedIds,
        CancellationToken cancellationToken = default)
    {
        ExecuteByIdsAsyncCalls.Add(requestedIds);
        return Task.FromResult<IReadOnlyList<ExecutionResult>>(_idResults);
    }
}

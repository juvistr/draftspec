using DraftSpec.Cli.History;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecHistoryService for testing.
/// </summary>
public class MockSpecHistoryService : ISpecHistoryService
{
    private SpecHistory _history = SpecHistory.Empty;
    private readonly List<SpecRunRecord> _recordedResults = new();
    private readonly HashSet<string> _quarantinedIds = new();
    private readonly List<FlakySpec> _flakySpecs = new();
    private bool _clearSpecResult = true;

    /// <summary>
    /// Gets all recorded results for assertions.
    /// </summary>
    public IReadOnlyList<SpecRunRecord> RecordedResults => _recordedResults;

    /// <summary>
    /// Gets the project paths passed to LoadAsync.
    /// </summary>
    public List<string> LoadAsyncCalls { get; } = [];

    /// <summary>
    /// Gets the cancellation tokens passed to LoadAsync.
    /// </summary>
    public List<CancellationToken> LoadAsyncCancellationTokens { get; } = [];

    /// <summary>
    /// Gets how many times SaveAsync was called.
    /// </summary>
    public int SaveAsyncCalls { get; private set; }

    /// <summary>
    /// Gets how many times RecordRunAsync was called.
    /// </summary>
    public int RecordRunAsyncCalls { get; private set; }

    /// <summary>
    /// Configure the history to return.
    /// </summary>
    public MockSpecHistoryService WithHistory(SpecHistory history)
    {
        _history = history;
        return this;
    }

    /// <summary>
    /// Configure quarantined spec IDs.
    /// </summary>
    public MockSpecHistoryService WithQuarantinedIds(params string[] ids)
    {
        foreach (var id in ids)
            _quarantinedIds.Add(id);
        return this;
    }

    /// <summary>
    /// Configure flaky specs to return.
    /// </summary>
    public MockSpecHistoryService WithFlakySpecs(params FlakySpec[] specs)
    {
        _flakySpecs.AddRange(specs);
        return this;
    }

    /// <summary>
    /// Configure the result of ClearSpecAsync.
    /// </summary>
    public MockSpecHistoryService WithClearSpecResult(bool result)
    {
        _clearSpecResult = result;
        return this;
    }

    public Task<SpecHistory> LoadAsync(string projectPath, CancellationToken ct = default)
    {
        LoadAsyncCalls.Add(projectPath);
        LoadAsyncCancellationTokens.Add(ct);
        return Task.FromResult(_history);
    }

    public Task SaveAsync(string projectPath, SpecHistory history, CancellationToken ct = default)
    {
        SaveAsyncCalls++;
        _history = history;
        return Task.CompletedTask;
    }

    public Task RecordRunAsync(string projectPath, IReadOnlyList<SpecRunRecord> results, CancellationToken ct = default)
    {
        RecordRunAsyncCalls++;
        _recordedResults.AddRange(results);
        return Task.CompletedTask;
    }

    public IReadOnlyList<FlakySpec> GetFlakySpecs(SpecHistory history, int minStatusChanges = 2, int windowSize = 10)
    {
        return _flakySpecs;
    }

    public IReadOnlySet<string> GetQuarantinedSpecIds(SpecHistory history, int minStatusChanges = 2, int windowSize = 10)
    {
        return _quarantinedIds;
    }

    public Task<bool> ClearSpecAsync(string projectPath, string specId, CancellationToken ct = default)
    {
        return Task.FromResult(_clearSpecResult);
    }
}

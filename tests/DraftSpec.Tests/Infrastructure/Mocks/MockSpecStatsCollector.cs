using DraftSpec.Cli;
using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecStatsCollector for testing.
/// </summary>
public class MockSpecStatsCollector : ISpecStatsCollector
{
    private SpecStats? _stats;
    private Exception? _exception;

    public List<(IReadOnlyList<string> SpecFiles, string ProjectPath)> CollectAsyncCalls { get; } = [];

    /// <summary>
    /// Configure the stats result returned by CollectAsync.
    /// </summary>
    public MockSpecStatsCollector WithStats(SpecStats stats)
    {
        _stats = stats;
        return this;
    }

    /// <summary>
    /// Configure the collector to throw an exception.
    /// </summary>
    public MockSpecStatsCollector Throws(Exception exception)
    {
        _exception = exception;
        return this;
    }

    public Task<SpecStats> CollectAsync(
        IReadOnlyList<string> specFiles,
        string projectPath,
        CancellationToken ct = default)
    {
        CollectAsyncCalls.Add((specFiles, projectPath));

        if (_exception != null)
            throw _exception;

        return Task.FromResult(_stats ?? new SpecStats(
            Total: 0,
            Regular: 0,
            Focused: 0,
            Skipped: 0,
            Pending: 0,
            HasFocusMode: false,
            FileCount: 0));
    }
}

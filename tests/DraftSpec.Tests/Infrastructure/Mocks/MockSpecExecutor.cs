using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecExecutor for testing orchestration logic.
/// </summary>
public class MockSpecExecutor : ISpecExecutor
{
    private readonly RunSpecResult _result;
    private readonly List<ExecutionRecord> _executions = [];

    public MockSpecExecutor(RunSpecResult result)
    {
        _result = result;
    }

    /// <summary>
    /// Creates a mock executor that returns a successful result.
    /// </summary>
    public static MockSpecExecutor Successful() => new(new RunSpecResult
    {
        Success = true,
        ExitCode = 0,
        DurationMs = 100
    });

    /// <summary>
    /// Creates a mock executor that returns a failed result.
    /// </summary>
    public static MockSpecExecutor Failed(string? error = null) => new(new RunSpecResult
    {
        Success = false,
        ExitCode = 1,
        Error = error != null ? new SpecError { Category = ErrorCategory.Runtime, Message = error } : null,
        DurationMs = 100
    });

    /// <summary>
    /// Records of all executions made.
    /// </summary>
    public IReadOnlyList<ExecutionRecord> Executions => _executions;

    /// <summary>
    /// The content passed to the last execution.
    /// </summary>
    public string? LastContent => _executions.LastOrDefault()?.Content;

    /// <summary>
    /// The timeout passed to the last execution.
    /// </summary>
    public TimeSpan? LastTimeout => _executions.LastOrDefault()?.Timeout;

    /// <summary>
    /// The number of times ExecuteAsync was called.
    /// </summary>
    public int ExecutionCount => _executions.Count;

    public Task<RunSpecResult> ExecuteAsync(
        string content,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        _executions.Add(new ExecutionRecord(content, timeout));
        return Task.FromResult(_result);
    }

    public record ExecutionRecord(string Content, TimeSpan Timeout);
}

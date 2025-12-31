using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecExecutionService for testing.
/// </summary>
public class MockSpecExecutionService : ISpecExecutionService
{
    private readonly RunSpecResult _result;
    private readonly List<SpecProgressNotification>? _progressNotifications;

    public int ExecutionCount { get; private set; }
    public string? LastContent { get; private set; }
    public TimeSpan? LastTimeout { get; private set; }
    public CancellationToken? LastCancellationToken { get; private set; }
    public bool WasProgressCallbackProvided { get; private set; }

    private MockSpecExecutionService(RunSpecResult result, List<SpecProgressNotification>? progressNotifications = null)
    {
        _result = result;
        _progressNotifications = progressNotifications;
    }

    public static MockSpecExecutionService Successful(List<SpecProgressNotification>? progressNotifications = null)
    {
        return new MockSpecExecutionService(new RunSpecResult
        {
            Success = true,
            ExitCode = 0,
            DurationMs = 100
        }, progressNotifications);
    }

    public static MockSpecExecutionService Failed(string errorMessage)
    {
        return new MockSpecExecutionService(new RunSpecResult
        {
            Success = false,
            ExitCode = 1,
            Error = new SpecError { Message = errorMessage },
            DurationMs = 100
        });
    }

    public Task<RunSpecResult> ExecuteSpecAsync(
        string specContent,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ExecutionCount++;
        LastContent = specContent;
        LastTimeout = timeout;
        LastCancellationToken = cancellationToken;
        WasProgressCallbackProvided = false;

        return Task.FromResult(_result);
    }

    public async Task<RunSpecResult> ExecuteSpecAsync(
        string specContent,
        TimeSpan timeout,
        Func<SpecProgressNotification, Task>? onProgress,
        CancellationToken cancellationToken)
    {
        ExecutionCount++;
        LastContent = specContent;
        LastTimeout = timeout;
        LastCancellationToken = cancellationToken;
        WasProgressCallbackProvided = onProgress != null;

        // Emit progress notifications if provided
        if (onProgress != null && _progressNotifications != null)
        {
            foreach (var notification in _progressNotifications)
            {
                await onProgress(notification);
            }
        }

        return _result;
    }
}

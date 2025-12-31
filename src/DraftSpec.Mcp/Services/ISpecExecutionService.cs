using DraftSpec.Mcp.Models;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Interface for executing DraftSpec tests.
/// </summary>
public interface ISpecExecutionService
{
    /// <summary>
    /// Execute spec content and return structured results.
    /// </summary>
    Task<RunSpecResult> ExecuteSpecAsync(
        string specContent,
        TimeSpan timeout,
        CancellationToken cancellationToken);

    /// <summary>
    /// Execute spec content and return structured results with progress notifications.
    /// </summary>
    Task<RunSpecResult> ExecuteSpecAsync(
        string specContent,
        TimeSpan timeout,
        Func<SpecProgressNotification, Task>? onProgress,
        CancellationToken cancellationToken);
}

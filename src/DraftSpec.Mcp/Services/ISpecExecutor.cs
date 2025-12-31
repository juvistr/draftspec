using DraftSpec.Mcp.Models;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Interface for executing spec content and returning results.
/// Implemented by SubprocessSpecExecutor for production use.
/// </summary>
public interface ISpecExecutor
{
    /// <summary>
    /// Executes the spec content and returns the result.
    /// </summary>
    /// <param name="content">The spec content to execute.</param>
    /// <param name="timeout">Maximum execution time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<RunSpecResult> ExecuteAsync(
        string content,
        TimeSpan timeout,
        CancellationToken ct = default);
}

using DraftSpec.Mcp.Models;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Result of a spec run orchestration.
/// Contains either a successful result or an error.
/// </summary>
public record SpecRunOrchestratorResult
{
    /// <summary>
    /// Whether the orchestration completed successfully (session found, execution completed).
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if orchestration failed (e.g., session not found).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The execution result (null if orchestration failed).
    /// </summary>
    public RunSpecResult? Result { get; init; }

    /// <summary>
    /// Session ID if a session was used.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Length of accumulated content if a session was used.
    /// </summary>
    public int? AccumulatedContentLength { get; init; }

    /// <summary>
    /// Creates a result for when a session was not found.
    /// </summary>
    public static SpecRunOrchestratorResult SessionNotFound(string sessionId) => new()
    {
        IsSuccess = false,
        Error = $"Session '{sessionId}' not found or has expired. Create a new session with create_session."
    };

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static SpecRunOrchestratorResult FromResult(RunSpecResult result, Session? session) => new()
    {
        IsSuccess = true,
        Result = result,
        SessionId = session?.Id,
        AccumulatedContentLength = session?.AccumulatedContent.Length
    };

    /// <summary>
    /// Converts this result to a JSON-serializable response object.
    /// </summary>
    public object ToResponse()
    {
        if (!IsSuccess)
        {
            return new { success = false, error = Error };
        }

        if (SessionId != null)
        {
            return new
            {
                Result!.Success,
                Result.Report,
                Result.ConsoleOutput,
                Result.ErrorOutput,
                Result.ExitCode,
                Result.DurationMs,
                sessionId = SessionId,
                accumulatedContentLength = AccumulatedContentLength
            };
        }

        return Result!;
    }
}

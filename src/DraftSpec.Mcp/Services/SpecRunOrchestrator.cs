namespace DraftSpec.Mcp.Services;

/// <summary>
/// Orchestrates spec execution with session management.
/// Handles session resolution, content combination, execution delegation,
/// and content accumulation on success.
/// </summary>
public class SpecRunOrchestrator
{
    private readonly SessionManager _sessionManager;

    /// <summary>
    /// Minimum allowed timeout in seconds.
    /// </summary>
    public const int MinTimeoutSeconds = 1;

    /// <summary>
    /// Maximum allowed timeout in seconds.
    /// </summary>
    public const int MaxTimeoutSeconds = 60;

    public SpecRunOrchestrator(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    /// <summary>
    /// Runs a spec with optional session management.
    /// </summary>
    /// <param name="executor">The spec executor to use.</param>
    /// <param name="specContent">The spec content to execute.</param>
    /// <param name="sessionId">Optional session ID for content accumulation.</param>
    /// <param name="timeoutSeconds">Timeout in seconds (clamped to 1-60).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The orchestration result.</returns>
    public async Task<SpecRunOrchestratorResult> RunAsync(
        ISpecExecutor executor,
        string specContent,
        string? sessionId,
        int timeoutSeconds,
        CancellationToken ct = default)
    {
        // 1. Clamp timeout
        timeoutSeconds = Math.Clamp(timeoutSeconds, MinTimeoutSeconds, MaxTimeoutSeconds);

        // 2. Resolve session
        Session? session = null;
        string effectiveContent = specContent;

        if (!string.IsNullOrEmpty(sessionId))
        {
            session = _sessionManager.GetSession(sessionId);
            if (session == null)
            {
                return SpecRunOrchestratorResult.SessionNotFound(sessionId);
            }
            effectiveContent = session.GetFullContent(specContent);
        }

        // 3. Execute
        var result = await executor.ExecuteAsync(
            effectiveContent,
            TimeSpan.FromSeconds(timeoutSeconds),
            ct).ConfigureAwait(false);

        // 4. Accumulate on success
        if (session != null && result.Success)
        {
            session.AppendContent(specContent);
        }

        // 5. Return with session info
        return SpecRunOrchestratorResult.FromResult(result, session);
    }
}

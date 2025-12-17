using System.ComponentModel;
using System.Text.Json;
using DraftSpec.Formatters;
using DraftSpec.Mcp.Services;
using ModelContextProtocol.Server;

namespace DraftSpec.Mcp.Tools;

/// <summary>
/// MCP tools for session management in multi-turn workflows.
/// </summary>
[McpServerToolType]
public static class SessionTools
{
    /// <summary>
    /// Create a new session for accumulating specs across multiple tool calls.
    /// </summary>
    [McpServerTool(Name = "create_session")]
    [Description("Create a new session for multi-turn spec development. " +
                 "Sessions accumulate spec content across calls, enabling iterative development. " +
                 "Use session_id with run_spec to build specs incrementally.")]
    public static string CreateSession(
        SessionManager sessionManager,
        [Description("Session timeout in minutes (default: 30, max: 120). Session expires after inactivity.")]
        int? timeoutMinutes = null)
    {
        if (timeoutMinutes.HasValue)
        {
            timeoutMinutes = Math.Clamp(timeoutMinutes.Value, 1, 120);
        }

        var session = sessionManager.CreateSession(timeoutMinutes);

        var result = new
        {
            sessionId = session.Id,
            timeoutMinutes = (int)session.Timeout.TotalMinutes,
            expiresAt = session.LastAccessedAt.Add(session.Timeout),
            message = "Session created. Use this session_id with run_spec to accumulate specs."
        };

        return JsonSerializer.Serialize(result, JsonOptionsProvider.Default);
    }

    /// <summary>
    /// End a session and clean up resources.
    /// </summary>
    [McpServerTool(Name = "dispose_session")]
    [Description("End a session and clean up its resources. " +
                 "Use when done with iterative spec development.")]
    public static string DisposeSession(
        SessionManager sessionManager,
        [Description("The session ID to dispose")]
        string sessionId)
    {
        var disposed = sessionManager.DisposeSession(sessionId);

        var result = new
        {
            success = disposed,
            message = disposed
                ? $"Session {sessionId} has been disposed."
                : $"Session {sessionId} not found (may have expired)."
        };

        return JsonSerializer.Serialize(result, JsonOptionsProvider.Default);
    }

    /// <summary>
    /// List all active sessions.
    /// </summary>
    [McpServerTool(Name = "list_sessions")]
    [Description("List all active sessions with their status and accumulated content info.")]
    public static string ListSessions(SessionManager sessionManager)
    {
        var sessions = sessionManager.GetAllSessions();

        var result = new
        {
            count = sessions.Count,
            sessions = sessions.Select(s => new
            {
                sessionId = s.Id,
                createdAt = s.CreatedAt,
                lastAccessedAt = s.LastAccessedAt,
                timeoutMinutes = s.TimeoutMinutes,
                hasAccumulatedContent = s.HasAccumulatedContent
            })
        };

        return JsonSerializer.Serialize(result, JsonOptionsProvider.Default);
    }
}

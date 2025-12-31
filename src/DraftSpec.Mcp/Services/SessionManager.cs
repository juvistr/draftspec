using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Manages sessions for multi-turn spec execution workflows.
/// Handles session creation, retrieval, expiration, and cleanup.
/// </summary>
public class SessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly ILogger<SessionManager> _logger;
    private readonly string _baseTempDirectory;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _defaultTimeout;
    private readonly TimeSpan _cleanupInterval;
    private bool _disposed;

    /// <summary>
    /// Default session timeout (30 minutes).
    /// </summary>
    public static readonly TimeSpan DefaultSessionTimeout = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Default cleanup interval (5 minutes).
    /// </summary>
    public static readonly TimeSpan DefaultCleanupInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Create a new SessionManager.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="baseTempDirectory">Base directory for session temp files (optional)</param>
    /// <param name="defaultTimeout">Default session timeout (optional)</param>
    /// <param name="cleanupInterval">Interval for cleanup checks (optional)</param>
    public SessionManager(
        ILogger<SessionManager> logger,
        string? baseTempDirectory = null,
        TimeSpan? defaultTimeout = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _baseTempDirectory = baseTempDirectory ?? Path.Combine(Path.GetTempPath(), "draftspec-sessions");
        _defaultTimeout = defaultTimeout ?? DefaultSessionTimeout;
        _cleanupInterval = cleanupInterval ?? DefaultCleanupInterval;

        // Ensure base directory exists
        Directory.CreateDirectory(_baseTempDirectory);

        // Start cleanup timer
        _cleanupTimer = new Timer(CleanupExpiredSessions, null, _cleanupInterval, _cleanupInterval);
    }

    /// <summary>
    /// Number of active sessions.
    /// </summary>
    public int ActiveSessionCount => _sessions.Count;

    /// <summary>
    /// Create a new session with optional custom timeout.
    /// </summary>
    /// <param name="timeoutMinutes">Session timeout in minutes (optional, uses default if not specified)</param>
    /// <returns>The created session</returns>
    public Session CreateSession(int? timeoutMinutes = null)
    {
        var id = GenerateSessionId();
        var timeout = timeoutMinutes.HasValue
            ? TimeSpan.FromMinutes(timeoutMinutes.Value)
            : _defaultTimeout;
        var tempDir = Path.Combine(_baseTempDirectory, id);

        var session = new Session(id, timeout, tempDir);
        _sessions[id] = session;

        _logger.LogInformation("Created session {SessionId} with timeout {Timeout}m", id, timeout.TotalMinutes);
        return session;
    }

    /// <summary>
    /// Get a session by ID.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>The session if found and not expired, null otherwise</returns>
    public Session? GetSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;

        if (!_sessions.TryGetValue(sessionId, out var session))
            return null;

        if (session.IsExpired)
        {
            _logger.LogInformation("Session {SessionId} has expired", sessionId);
            DisposeSession(sessionId);
            return null;
        }

        session.Touch();
        return session;
    }

    /// <summary>
    /// Dispose a session and clean up its resources.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>True if session was found and disposed</returns>
    public bool DisposeSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;

        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
            _logger.LogInformation("Disposed session {SessionId}", sessionId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get information about all active sessions.
    /// </summary>
    /// <returns>List of session info</returns>
    public IReadOnlyList<SessionInfo> GetAllSessions()
    {
        return _sessions.Values
            .Where(s => !s.IsExpired)
            .Select(s => new SessionInfo
            {
                Id = s.Id,
                CreatedAt = s.CreatedAt,
                LastAccessedAt = s.LastAccessedAt,
                TimeoutMinutes = (int)s.Timeout.TotalMinutes,
                HasAccumulatedContent = !string.IsNullOrEmpty(s.AccumulatedContent),
                IsExpired = s.IsExpired
            })
            .ToList();
    }

    private void CleanupExpiredSessions(object? state)
    {
        var expiredIds = _sessions
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in expiredIds)
        {
            DisposeSession(id);
        }

        if (expiredIds.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredIds.Count);
        }
    }

    private static string GenerateSessionId()
    {
        // Generate a cryptographically secure session ID using RandomNumberGenerator
        // 16 bytes = 128 bits of entropy, more than GUID's 122 bits
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return $"session-{Convert.ToHexString(bytes)}";
    }

    /// <summary>
    /// Dispose the session manager and all sessions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cleanupTimer.Dispose();

        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }
        _sessions.Clear();

        // Try to clean up base directory
        try
        {
            if (Directory.Exists(_baseTempDirectory) &&
                !Directory.EnumerateFileSystemEntries(_baseTempDirectory).Any())
            {
                Directory.Delete(_baseTempDirectory);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }
}

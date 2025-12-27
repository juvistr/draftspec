using DraftSpec.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for SessionManager class.
/// </summary>
public class SessionManagerTests
{
    private readonly ILogger<SessionManager> _logger = new NullLogger<SessionManager>();
    private readonly string _baseTempDir;

    /// <summary>
    /// Simple no-op logger for testing.
    /// </summary>
    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    public SessionManagerTests()
    {
        _baseTempDir = Path.Combine(Path.GetTempPath(), $"session-manager-tests-{Guid.NewGuid()}");
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_baseTempDir))
        {
            try { Directory.Delete(_baseTempDir, recursive: true); }
            catch { /* ignore */ }
        }
    }

    [Test]
    public async Task CreateSession_ReturnsNewSession()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var session = manager.CreateSession();

        await Assert.That(session).IsNotNull();
        await Assert.That(session.Id).IsNotEmpty();
    }

    [Test]
    public async Task CreateSession_UsesDefaultTimeout()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var session = manager.CreateSession();

        await Assert.That(session.Timeout).IsEqualTo(SessionManager.DefaultSessionTimeout);
    }

    [Test]
    public async Task CreateSession_UsesCustomTimeout()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var session = manager.CreateSession(timeoutMinutes: 60);

        await Assert.That(session.Timeout).IsEqualTo(TimeSpan.FromMinutes(60));
    }

    [Test]
    public async Task CreateSession_IncrementsActiveCount()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        await Assert.That(manager.ActiveSessionCount).IsEqualTo(0);

        manager.CreateSession();
        await Assert.That(manager.ActiveSessionCount).IsEqualTo(1);

        manager.CreateSession();
        await Assert.That(manager.ActiveSessionCount).IsEqualTo(2);
    }

    [Test]
    public async Task GetSession_ReturnsSessionById()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);
        var created = manager.CreateSession();

        var retrieved = manager.GetSession(created.Id);

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Id).IsEqualTo(created.Id);
    }

    [Test]
    public async Task GetSession_ReturnsNullForUnknownId()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var session = manager.GetSession("unknown-session-id");

        await Assert.That(session).IsNull();
    }

    [Test]
    public async Task GetSession_ReturnsNullForEmptyId()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var nullId = manager.GetSession(null!);
        var emptyId = manager.GetSession("");

        await Assert.That(nullId).IsNull();
        await Assert.That(emptyId).IsNull();
    }

    [Test]
    public async Task GetSession_ReturnsNullForExpiredSession()
    {
        using var manager = new SessionManager(_logger, _baseTempDir,
            defaultTimeout: TimeSpan.FromMilliseconds(1));

        var created = manager.CreateSession();
        await Task.Delay(50);

        var retrieved = manager.GetSession(created.Id);

        await Assert.That(retrieved).IsNull();
    }

    [Test]
    public async Task GetSession_TouchesSession()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);
        var session = manager.CreateSession();
        var originalLastAccessed = session.LastAccessedAt;

        await Task.Delay(10);
        manager.GetSession(session.Id);

        await Assert.That(session.LastAccessedAt).IsGreaterThan(originalLastAccessed);
    }

    [Test]
    public async Task DisposeSession_RemovesSession()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);
        var session = manager.CreateSession();

        var disposed = manager.DisposeSession(session.Id);

        await Assert.That(disposed).IsTrue();
        await Assert.That(manager.ActiveSessionCount).IsEqualTo(0);
        await Assert.That(manager.GetSession(session.Id)).IsNull();
    }

    [Test]
    public async Task DisposeSession_ReturnsFalseForUnknownId()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var disposed = manager.DisposeSession("unknown-id");

        await Assert.That(disposed).IsFalse();
    }

    [Test]
    public async Task DisposeSession_ReturnsFalseForEmptyId()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var nullId = manager.DisposeSession(null!);
        var emptyId = manager.DisposeSession("");

        await Assert.That(nullId).IsFalse();
        await Assert.That(emptyId).IsFalse();
    }

    [Test]
    public async Task GetAllSessions_ReturnsActiveSessionInfo()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);
        var session1 = manager.CreateSession(timeoutMinutes: 30);
        var session2 = manager.CreateSession(timeoutMinutes: 60);
        session1.AppendContent("some content");

        var sessions = manager.GetAllSessions();

        await Assert.That(sessions.Count).IsEqualTo(2);

        var info1 = sessions.Single(s => s.Id == session1.Id);
        await Assert.That(info1.TimeoutMinutes).IsEqualTo(30);
        await Assert.That(info1.HasAccumulatedContent).IsTrue();
        await Assert.That(info1.IsExpired).IsFalse();

        var info2 = sessions.Single(s => s.Id == session2.Id);
        await Assert.That(info2.TimeoutMinutes).IsEqualTo(60);
        await Assert.That(info2.HasAccumulatedContent).IsFalse();
    }

    [Test]
    [Retry(3)] // Timing-sensitive test, retry if flaky
    public async Task GetAllSessions_ExcludesExpiredSessions()
    {
        using var manager = new SessionManager(_logger, _baseTempDir,
            defaultTimeout: TimeSpan.FromMilliseconds(10));

        manager.CreateSession();
        await Task.Delay(100);

        var sessions = manager.GetAllSessions();

        await Assert.That(sessions.Count).IsEqualTo(0);
    }

    [Test]
    [Retry(3)] // Timing-sensitive test, retry if flaky
    public async Task CleanupTimer_RemovesExpiredSessions()
    {
        using var manager = new SessionManager(_logger, _baseTempDir,
            defaultTimeout: TimeSpan.FromMilliseconds(50),
            cleanupInterval: TimeSpan.FromMilliseconds(100));

        manager.CreateSession();
        await Assert.That(manager.ActiveSessionCount).IsEqualTo(1);

        // Wait for session to expire and cleanup timer to run
        // Use longer delay to account for CI timing variability
        await Task.Delay(500);

        await Assert.That(manager.ActiveSessionCount).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_CleansUpAllSessions()
    {
        var manager = new SessionManager(_logger, _baseTempDir);
        manager.CreateSession();
        manager.CreateSession();

        manager.Dispose();

        // Should not throw even after disposal
        await Assert.That(manager.ActiveSessionCount).IsEqualTo(0);
    }

    [Test]
    public async Task SessionIdFormat_IsReadable()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var session = manager.CreateSession();

        await Assert.That(session.Id).StartsWith("session-");
        await Assert.That(session.Id.Length).IsGreaterThan(20);
    }

    [Test]
    public async Task SessionIds_AreUnique()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);
        var ids = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            var session = manager.CreateSession();
            ids.Add(session.Id);
        }

        await Assert.That(ids.Count).IsEqualTo(100);
    }

    [Test]
    public async Task SessionId_HasNoPredictableTimestamp()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var session = manager.CreateSession();

        // Session ID should not contain date/time patterns
        // Old format was: session-{yyyyMMdd}-{HHmmss}-{guid8}
        // New format is: session-{guid32}
        var id = session.Id;
        var parts = id.Split('-');

        // Should only have 2 parts: "session" and the GUID (no hyphens in :N format)
        await Assert.That(parts.Length).IsEqualTo(2);
        await Assert.That(parts[0]).IsEqualTo("session");
        await Assert.That(parts[1].Length).IsEqualTo(32); // GUID without hyphens
    }

    [Test]
    public async Task SessionId_UsesFullGuidEntropy()
    {
        using var manager = new SessionManager(_logger, _baseTempDir);

        var session = manager.CreateSession();

        // Extract the GUID part (after "session-")
        var guidPart = session.Id["session-".Length..];

        // Should be valid hex string of 32 chars (128 bits = 16 bytes = 32 hex chars)
        await Assert.That(guidPart.Length).IsEqualTo(32);
        await Assert.That(guidPart.All(c => char.IsAsciiHexDigit(c))).IsTrue();
    }
}

using DraftSpec.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Tests.Integration;

/// <summary>
/// Integration tests for MCP-Core workflows.
/// Tests session lifecycle and management.
/// </summary>
public class McpCoreIntegrationTests
{
    private readonly ILogger<SessionManager> _sessionLogger = new NullLogger<SessionManager>();
    private string _baseTempDir = null!;

    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Before(Test)]
    public void SetUp()
    {
        _baseTempDir = Path.Combine(Path.GetTempPath(), $"McpCoreIntegration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_baseTempDir);
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_baseTempDir))
        {
            try { Directory.Delete(_baseTempDir, true); }
            catch { /* ignore cleanup errors */ }
        }
    }

    #region Session Lifecycle

    [Test]
    public async Task SessionLifecycle_CreateExecuteDispose_WorksCorrectly()
    {
        using var sessionManager = new SessionManager(_sessionLogger, _baseTempDir);

        // Create session
        var session = sessionManager.CreateSession();
        await Assert.That(session).IsNotNull();
        await Assert.That(sessionManager.ActiveSessionCount).IsEqualTo(1);

        // Accumulate content
        session.AppendContent("""
            describe("Test", () => {
                it("passes", () => { });
            });
            """);

        await Assert.That(session.AccumulatedContent).Contains("describe");

        // Dispose session
        var disposed = sessionManager.DisposeSession(session.Id);
        await Assert.That(disposed).IsTrue();
        await Assert.That(sessionManager.ActiveSessionCount).IsEqualTo(0);
    }

    [Test]
    public async Task MultiSession_ConcurrentCreation_IsolatesSessions()
    {
        using var sessionManager = new SessionManager(_sessionLogger, _baseTempDir);

        // Create multiple sessions concurrently
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            var session = sessionManager.CreateSession();
            session.AppendContent($"// Session {i} content");
            return session;
        }).ToList();

        var sessions = await Task.WhenAll(tasks);

        // Verify all sessions are unique
        await Assert.That(sessions.Select(s => s.Id).Distinct().Count()).IsEqualTo(5);
        await Assert.That(sessionManager.ActiveSessionCount).IsEqualTo(5);

        // Verify content isolation
        for (var i = 0; i < 5; i++)
        {
            await Assert.That(sessions[i].AccumulatedContent).Contains($"Session {i}");
        }
    }

    #endregion
}

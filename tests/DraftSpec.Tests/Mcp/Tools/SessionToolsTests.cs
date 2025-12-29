using System.Text.Json;
using DraftSpec.Mcp.Services;
using DraftSpec.Mcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DraftSpec.Tests.Mcp.Tools;

/// <summary>
/// Integration tests for SessionTools MCP methods.
/// </summary>
public class SessionToolsTests : IDisposable
{
    private readonly SessionManager _sessionManager;

    public SessionToolsTests()
    {
        _sessionManager = new SessionManager(
            NullLogger<SessionManager>.Instance);
    }

    public void Dispose()
    {
        _sessionManager.Dispose();
    }

    private static SessionManager CreateFreshManager() =>
        new(NullLogger<SessionManager>.Instance);

    #region CreateSession

    [Test]
    public async Task CreateSession_ReturnsSessionId()
    {
        var result = SessionTools.CreateSession(_sessionManager);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsNotNull();
        await Assert.That(sessionId.GetString()!.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task CreateSession_DefaultTimeout_Is30Minutes()
    {
        var result = SessionTools.CreateSession(_sessionManager);
        var json = JsonDocument.Parse(result);

        var timeout = json.RootElement.GetProperty("timeoutMinutes").GetInt32();
        await Assert.That(timeout).IsEqualTo(30);
    }

    [Test]
    public async Task CreateSession_CustomTimeout_IsRespected()
    {
        var result = SessionTools.CreateSession(_sessionManager, timeoutMinutes: 60);
        var json = JsonDocument.Parse(result);

        var timeout = json.RootElement.GetProperty("timeoutMinutes").GetInt32();
        await Assert.That(timeout).IsEqualTo(60);
    }

    [Test]
    public async Task CreateSession_TimeoutClamped_ToMaximum120()
    {
        var result = SessionTools.CreateSession(_sessionManager, timeoutMinutes: 200);
        var json = JsonDocument.Parse(result);

        var timeout = json.RootElement.GetProperty("timeoutMinutes").GetInt32();
        await Assert.That(timeout).IsEqualTo(120);
    }

    [Test]
    public async Task CreateSession_TimeoutClamped_ToMinimum1()
    {
        var result = SessionTools.CreateSession(_sessionManager, timeoutMinutes: 0);
        var json = JsonDocument.Parse(result);

        var timeout = json.RootElement.GetProperty("timeoutMinutes").GetInt32();
        await Assert.That(timeout).IsEqualTo(1);
    }

    [Test]
    public async Task CreateSession_IncludesExpiresAt()
    {
        var result = SessionTools.CreateSession(_sessionManager);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("expiresAt", out _)).IsTrue();
    }

    [Test]
    public async Task CreateSession_IncludesMessage()
    {
        var result = SessionTools.CreateSession(_sessionManager);
        var json = JsonDocument.Parse(result);

        var message = json.RootElement.GetProperty("message").GetString();
        await Assert.That(message).Contains("session_id");
    }

    #endregion

    #region DisposeSession

    [Test]
    public async Task DisposeSession_ExistingSession_ReturnsSuccess()
    {
        // Create a session first
        var createResult = SessionTools.CreateSession(_sessionManager);
        var sessionId = JsonDocument.Parse(createResult).RootElement.GetProperty("sessionId").GetString()!;

        // Dispose it
        var disposeResult = SessionTools.DisposeSession(_sessionManager, sessionId);
        var json = JsonDocument.Parse(disposeResult);

        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task DisposeSession_NonExistentSession_ReturnsFalse()
    {
        var result = SessionTools.DisposeSession(_sessionManager, "non-existent-session");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsFalse();
        await Assert.That(json.RootElement.GetProperty("message").GetString()).Contains("not found");
    }

    [Test]
    public async Task DisposeSession_AlreadyDisposed_ReturnsFalse()
    {
        // Create and dispose
        var createResult = SessionTools.CreateSession(_sessionManager);
        var sessionId = JsonDocument.Parse(createResult).RootElement.GetProperty("sessionId").GetString()!;
        SessionTools.DisposeSession(_sessionManager, sessionId);

        // Try to dispose again
        var result = SessionTools.DisposeSession(_sessionManager, sessionId);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("success").GetBoolean()).IsFalse();
    }

    #endregion

    #region ListSessions

    [Test]
    public async Task ListSessions_NoSessions_ReturnsEmptyList()
    {
        // Use fresh manager
        var freshManager = CreateFreshManager();
        var result = SessionTools.ListSessions(freshManager);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("count").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task ListSessions_WithSessions_ReturnsAll()
    {
        var freshManager = CreateFreshManager();

        // Create multiple sessions
        SessionTools.CreateSession(freshManager);
        SessionTools.CreateSession(freshManager);
        SessionTools.CreateSession(freshManager);

        var result = SessionTools.ListSessions(freshManager);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("count").GetInt32()).IsEqualTo(3);
    }

    [Test]
    public async Task ListSessions_IncludesSessionDetails()
    {
        var freshManager = CreateFreshManager();
        SessionTools.CreateSession(freshManager);

        var result = SessionTools.ListSessions(freshManager);
        var json = JsonDocument.Parse(result);

        var sessions = json.RootElement.GetProperty("sessions");
        var firstSession = sessions.EnumerateArray().First();

        await Assert.That(firstSession.TryGetProperty("sessionId", out _)).IsTrue();
        await Assert.That(firstSession.TryGetProperty("createdAt", out _)).IsTrue();
        await Assert.That(firstSession.TryGetProperty("lastAccessedAt", out _)).IsTrue();
        await Assert.That(firstSession.TryGetProperty("timeoutMinutes", out _)).IsTrue();
        await Assert.That(firstSession.TryGetProperty("hasAccumulatedContent", out _)).IsTrue();
    }

    [Test]
    public async Task ListSessions_AfterDispose_CountDecreases()
    {
        var freshManager = CreateFreshManager();

        // Create sessions
        var createResult = SessionTools.CreateSession(freshManager);
        var sessionId = JsonDocument.Parse(createResult).RootElement.GetProperty("sessionId").GetString()!;
        SessionTools.CreateSession(freshManager);

        // Verify count is 2
        var listResult = SessionTools.ListSessions(freshManager);
        await Assert.That(JsonDocument.Parse(listResult).RootElement.GetProperty("count").GetInt32()).IsEqualTo(2);

        // Dispose one
        SessionTools.DisposeSession(freshManager, sessionId);

        // Verify count is 1
        listResult = SessionTools.ListSessions(freshManager);
        await Assert.That(JsonDocument.Parse(listResult).RootElement.GetProperty("count").GetInt32()).IsEqualTo(1);
    }

    #endregion
}

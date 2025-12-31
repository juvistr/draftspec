using DraftSpec.Mcp.Services;
using DraftSpec.Tests.Infrastructure.Mocks;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Tests.Mcp.Services;

/// <summary>
/// Tests for SpecRunOrchestrator class.
/// </summary>
public class SpecRunOrchestratorTests
{
    private readonly ILogger<SessionManager> _logger = new NullLogger<SessionManager>();
    private readonly string _baseTempDir;

    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    public SpecRunOrchestratorTests()
    {
        _baseTempDir = Path.Combine(Path.GetTempPath(), $"orchestrator-tests-{Guid.NewGuid()}");
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
    public async Task RunAsync_NoSession_ExecutesDirectly()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        var result = await orchestrator.RunAsync(executor, "describe(\"test\", () => {});", null, 10);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Result).IsNotNull();
        await Assert.That(result.SessionId).IsNull();
        await Assert.That(executor.ExecutionCount).IsEqualTo(1);
        await Assert.That(executor.LastContent).IsEqualTo("describe(\"test\", () => {});");
    }

    [Test]
    public async Task RunAsync_ValidSession_CombinesContent()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var session = sessionManager.CreateSession();
        session.AppendContent("// previous content\n");

        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        var result = await orchestrator.RunAsync(executor, "describe(\"new\", () => {});", session.Id, 10);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.SessionId).IsEqualTo(session.Id);
        await Assert.That(executor.LastContent).Contains("// previous content");
        await Assert.That(executor.LastContent).Contains("describe(\"new\", () => {});");
    }

    [Test]
    public async Task RunAsync_InvalidSession_ReturnsError()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        var result = await orchestrator.RunAsync(executor, "describe(\"test\", () => {});", "non-existent-session", 10);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("non-existent-session");
        await Assert.That(result.Error).Contains("not found");
        await Assert.That(executor.ExecutionCount).IsEqualTo(0); // Should not execute
    }

    [Test]
    public async Task RunAsync_SuccessfulRun_AccumulatesContent()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var session = sessionManager.CreateSession();
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        await orchestrator.RunAsync(executor, "describe(\"first\", () => {});", session.Id, 10);

        await Assert.That(session.AccumulatedContent).Contains("describe(\"first\", () => {});");
    }

    [Test]
    public async Task RunAsync_FailedRun_DoesNotAccumulate()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var session = sessionManager.CreateSession();
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Failed("test failure");

        await orchestrator.RunAsync(executor, "describe(\"failing\", () => {});", session.Id, 10);

        await Assert.That(session.AccumulatedContent).IsEmpty();
    }

    [Test]
    public async Task RunAsync_TimeoutClamped_ToMax60()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        await orchestrator.RunAsync(executor, "describe(\"test\", () => {});", null, 120);

        await Assert.That(executor.LastTimeout).IsEqualTo(TimeSpan.FromSeconds(60));
    }

    [Test]
    public async Task RunAsync_TimeoutClamped_ToMin1()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        await orchestrator.RunAsync(executor, "describe(\"test\", () => {});", null, -5);

        await Assert.That(executor.LastTimeout).IsEqualTo(TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task RunAsync_ReturnsAccumulatedContentLength()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var session = sessionManager.CreateSession();
        session.AppendContent("// existing\n");

        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        var result = await orchestrator.RunAsync(executor, "// new content", session.Id, 10);

        await Assert.That(result.AccumulatedContentLength).IsNotNull();
        await Assert.That(result.AccumulatedContentLength!.Value).IsGreaterThan(0);
    }

    [Test]
    public async Task RunAsync_EmptySessionId_TreatedAsNoSession()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        var result = await orchestrator.RunAsync(executor, "describe(\"test\", () => {});", "", 10);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.SessionId).IsNull();
    }

    [Test]
    public async Task RunAsync_PassesTimeoutToExecutor()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Successful();

        await orchestrator.RunAsync(executor, "describe(\"test\", () => {});", null, 30);

        await Assert.That(executor.LastTimeout).IsEqualTo(TimeSpan.FromSeconds(30));
    }

    [Test]
    public async Task RunAsync_PropagatesExecutionResult()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var orchestrator = new SpecRunOrchestrator(sessionManager);
        var executor = MockSpecExecutor.Failed("execution error");

        var result = await orchestrator.RunAsync(executor, "describe(\"test\", () => {});", null, 10);

        await Assert.That(result.IsSuccess).IsTrue(); // Orchestration succeeded
        await Assert.That(result.Result!.Success).IsFalse(); // But execution failed
        await Assert.That(result.Result!.Error!.Message).IsEqualTo("execution error");
    }
}

using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Mcp.Models;
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

    #region ToResponse Tests

    [Test]
    public async Task ToResponse_WhenNotSuccess_ReturnsErrorObject()
    {
        var result = SpecRunOrchestratorResult.SessionNotFound("test-session");

        var response = result.ToResponse();

        // Cast to dynamic to check properties
        var responseType = response.GetType();
        var successProp = responseType.GetProperty("success");
        var errorProp = responseType.GetProperty("error");

        await Assert.That(successProp).IsNotNull();
        await Assert.That(errorProp).IsNotNull();
        await Assert.That((bool)successProp!.GetValue(response)!).IsFalse();
        await Assert.That((string)errorProp!.GetValue(response)!).Contains("test-session");
    }

    [Test]
    public async Task ToResponse_WithSession_ReturnsSessionInfo()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var session = sessionManager.CreateSession();
        session.AppendContent("// test content");

        var runResult = new RunSpecResult
        {
            Success = true,
            Report = new SpecReport { Summary = new SpecSummary { Total = 1, Passed = 1 } },
            ConsoleOutput = "output",
            ErrorOutput = "",
            ExitCode = 0,
            DurationMs = 100
        };

        var result = SpecRunOrchestratorResult.FromResult(runResult, session);
        var response = result.ToResponse();

        var responseType = response.GetType();
        var sessionIdProp = responseType.GetProperty("sessionId");
        var accumulatedLengthProp = responseType.GetProperty("accumulatedContentLength");

        await Assert.That(sessionIdProp).IsNotNull();
        await Assert.That(accumulatedLengthProp).IsNotNull();
        await Assert.That((string)sessionIdProp!.GetValue(response)!).IsEqualTo(session.Id);
        var accumulatedLength = (int?)accumulatedLengthProp!.GetValue(response);
        await Assert.That(accumulatedLength).IsNotNull();
        await Assert.That(accumulatedLength!.Value).IsGreaterThan(0);
    }

    [Test]
    public async Task ToResponse_WithoutSession_ReturnsResultDirectly()
    {
        var runResult = new RunSpecResult
        {
            Success = true,
            Report = new SpecReport { Summary = new SpecSummary { Total = 2, Passed = 2 } },
            ConsoleOutput = "console output",
            ErrorOutput = "",
            ExitCode = 0,
            DurationMs = 50
        };

        var result = SpecRunOrchestratorResult.FromResult(runResult, null);
        var response = result.ToResponse();

        // When no session, should return the RunSpecResult directly
        await Assert.That(response).IsTypeOf<RunSpecResult>();
        var typedResponse = (RunSpecResult)response;
        await Assert.That(typedResponse.Success).IsTrue();
        await Assert.That(typedResponse.Report!.Summary.Passed).IsEqualTo(2);
    }

    [Test]
    public async Task ToResponse_WithSessionAndFailedRun_ReturnsSessionInfo()
    {
        using var sessionManager = new SessionManager(_logger, _baseTempDir);
        var session = sessionManager.CreateSession();

        var runResult = new RunSpecResult
        {
            Success = false,
            Report = new SpecReport { Summary = new SpecSummary { Total = 1, Failed = 1 } },
            Error = new SpecError { Message = "Assertion failed" },
            ExitCode = 1,
            DurationMs = 100
        };

        var result = SpecRunOrchestratorResult.FromResult(runResult, session);
        var response = result.ToResponse();

        var responseType = response.GetType();
        var successProp = responseType.GetProperty("Success");
        var sessionIdProp = responseType.GetProperty("sessionId");

        await Assert.That(successProp).IsNotNull();
        await Assert.That(sessionIdProp).IsNotNull();
        await Assert.That((bool)successProp!.GetValue(response)!).IsFalse();
    }

    #endregion
}

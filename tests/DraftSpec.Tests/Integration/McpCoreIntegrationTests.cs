using System.Collections.Concurrent;
using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Tests.Integration;

/// <summary>
/// Integration tests for MCP-Core workflows.
/// Tests session lifecycle, spec execution, and error propagation.
/// </summary>
public class McpCoreIntegrationTests
{
    private readonly ILogger<InProcessSpecRunner> _runnerLogger = new NullLogger<InProcessSpecRunner>();
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

    #region Session Lifecycle with Spec Execution

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

    // Note: Session expiry tests removed - timing-dependent tests are inherently flaky
    // Session expiry functionality is covered by unit tests with mocked time

    #endregion

    #region In-Process Spec Execution

    [Test]
    public async Task InProcessRunner_SimpleSpec_ExecutesSuccessfully()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);

        var specContent = """
            describe("Math", () => {
                it("adds numbers", () => {
                    if (2 + 2 != 4) throw new Exception("Math broken");
                });
            });
            """;

        var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Report).IsNotNull();
        await Assert.That(result.Report!.Summary.Total).IsEqualTo(1);
        await Assert.That(result.Report.Summary.Passed).IsEqualTo(1);
    }

    [Test]
    public async Task InProcessRunner_FailingSpec_CapturesError()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);

        var specContent = """
            describe("Failing", () => {
                it("fails", () => {
                    throw new Exception("Test failure message");
                });
            });
            """;

        var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Report).IsNotNull();
        await Assert.That(result.Report!.Summary.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task InProcessRunner_CompilationError_ReturnsError()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);

        var specContent = """
            describe("Invalid", () => {
                this is not valid C# syntax!!!
            });
            """;

        var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error!.Category).IsEqualTo(ErrorCategory.Compilation);
    }

    // Note: Timeout tests are covered by unit tests in InProcessSpecRunnerTests.
    // In-process execution has limited ability to cancel running scripts due to
    // Roslyn scripting limitations with both sync and async operations.

    [Test]
    public async Task InProcessRunner_CachesCompiledScripts()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);

        var specContent = """
            describe("Cached", () => {
                it("runs", () => { });
            });
            """;

        // First execution - compiles
        await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);
        await Assert.That(runner.CacheCount).IsEqualTo(1);

        // Second execution - uses cache
        await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);
        await Assert.That(runner.CacheCount).IsEqualTo(1);

        // Different content - new compilation
        var differentContent = """
            describe("Different", () => {
                it("also runs", () => { });
            });
            """;
        await runner.ExecuteAsync(differentContent, TimeSpan.FromSeconds(10), CancellationToken.None);
        await Assert.That(runner.CacheCount).IsEqualTo(2);
    }

    #endregion

    #region Multi-Session Concurrent Execution

    [Test]
    public async Task MultiSession_ConcurrentExecution_IsolatesResults()
    {
        using var sessionManager = new SessionManager(_sessionLogger, _baseTempDir);
        var runner = new InProcessSpecRunner(_runnerLogger);
        var results = new ConcurrentDictionary<string, RunSpecResult>();

        // Create multiple sessions with different specs
        var tasks = new List<Task>();
        for (var i = 0; i < 5; i++)
        {
            var sessionNum = i;
            var session = sessionManager.CreateSession();

            var specContent = $$"""
                describe("Session-{{sessionNum}}", () => {
                    it("identifies correctly", () => {
                        // Unique test per session
                    });
                });
                """;

            tasks.Add(Task.Run(async () =>
            {
                var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);
                results[session.Id] = result;
            }));
        }

        await Task.WhenAll(tasks);

        // Verify all sessions executed successfully
        await Assert.That(results.Count).IsEqualTo(5);
        await Assert.That(results.Values.All(r => r.Success)).IsTrue();
    }

    [Test]
    public async Task MultiSession_SharedRunner_HandlesContention()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);
        var completedCount = 0;

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var specContent = $$"""
                describe("Concurrent-{{i}}", () => {
                    it("runs concurrently", async () => {
                        await System.Threading.Tasks.Task.Delay(50);
                    });
                });
                """;

            var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(30), CancellationToken.None);
            if (result.Success) Interlocked.Increment(ref completedCount);
        }).ToList();

        await Task.WhenAll(tasks);

        await Assert.That(completedCount).IsEqualTo(10);
    }

    #endregion

    #region Error Propagation from Core to MCP

    [Test]
    public async Task ErrorPropagation_RuntimeException_CapturedInResult()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);

        var specContent = """
            describe("Error Test", () => {
                it("throws runtime error", () => {
                    throw new InvalidOperationException("Runtime error occurred");
                });
            });
            """;

        var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Report).IsNotNull();
        // Error should be in the spec result, not the execution error
        await Assert.That(result.Report!.Summary.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task ErrorPropagation_AsyncException_HandledCorrectly()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);

        var specContent = """
            describe("Async Error", () => {
                it("fails asynchronously", async () => {
                    await System.Threading.Tasks.Task.Delay(10);
                    throw new Exception("Async failure");
                });
            });
            """;

        var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Report!.Summary.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task ErrorPropagation_NullReferenceException_CapturedGracefully()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);

        var specContent = """
            describe("Null Error", () => {
                it("dereferences null", () => {
                    string s = null;
                    var len = s.Length; // NullReferenceException
                });
            });
            """;

        var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        // Should not crash, should capture as failed spec
        await Assert.That(result.Report!.Summary.Failed).IsEqualTo(1);
    }

    #endregion

    #region Progress and Reporting

    // Note: Console output capture test removed - flaky when tests run in parallel
    // due to AsyncLocal console capture interleaving issues

    [Test]
    public async Task Reporting_DurationTracked()
    {
        var runner = new InProcessSpecRunner(_runnerLogger);

        var specContent = """
            describe("Duration", () => {
                it("takes some time", async () => {
                    await System.Threading.Tasks.Task.Delay(50);
                });
            });
            """;

        var result = await runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.DurationMs).IsGreaterThan(40);
    }

    #endregion
}

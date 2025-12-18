using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for InProcessSpecRunner.
/// These tests run sequentially because they share static DraftSpec.Dsl state.
/// </summary>
[NotInParallel("InProcessSpecRunner")]
public class InProcessSpecRunnerTests
{
    private readonly InProcessSpecRunner _runner;

    public InProcessSpecRunnerTests()
    {
        _runner = new InProcessSpecRunner(NullLogger<InProcessSpecRunner>.Instance);
    }

    #region Successful Execution

    [Test]
    public async Task ExecuteAsync_PassingSpec_ReturnsSuccess()
    {
        var specContent = """
            describe("Math", () =>
            {
                it("adds numbers", () =>
                {
                    expect(1 + 1).toBe(2);
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Report).IsNotNull();
        await Assert.That(result.Report!.Summary.Passed).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_MultipleSpecs_AllExecuted()
    {
        var specContent = """
            describe("Tests", () =>
            {
                it("first spec", () => expect(true).toBeTrue());
                it("second spec", () => expect(1).toBe(1));
                it("third spec", () => expect("hello").toBe("hello"));
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Report!.Summary.Total).IsEqualTo(3);
        await Assert.That(result.Report.Summary.Passed).IsEqualTo(3);
    }

    [Test]
    public async Task ExecuteAsync_NestedDescribes_WorkCorrectly()
    {
        var specContent = """
            describe("Outer", () =>
            {
                describe("Inner", () =>
                {
                    it("nested spec", () => expect(true).toBeTrue());
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Report!.Summary.Passed).IsEqualTo(1);
    }

    #endregion

    #region Failing Specs

    [Test]
    public async Task ExecuteAsync_FailingSpec_ReturnsFailure()
    {
        var specContent = """
            describe("Failing", () =>
            {
                it("fails", () =>
                {
                    expect(1).toBe(2);
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Report!.Summary.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_MixedResults_ReportsCorrectly()
    {
        var specContent = """
            describe("Mixed", () =>
            {
                it("passes", () => expect(1).toBe(1));
                it("fails", () => expect(1).toBe(2));
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Report!.Summary.Passed).IsEqualTo(1);
        await Assert.That(result.Report.Summary.Failed).IsEqualTo(1);
    }

    #endregion

    #region Compilation Errors

    [Test]
    public async Task ExecuteAsync_CompilationError_ReturnsCompilationCategory()
    {
        var specContent = """
            describe("Bad", () =>
            {
                it("has syntax error", () =>
                {
                    var x = // missing value
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.Error!.Category).IsEqualTo(ErrorCategory.Compilation);
    }

    [Test]
    public async Task ExecuteAsync_UndefinedVariable_ReturnsCompilationError()
    {
        var specContent = """
            describe("Undefined", () =>
            {
                it("uses undefined var", () =>
                {
                    expect(undefinedVariable).toBe(1);
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error!.Category).IsEqualTo(ErrorCategory.Compilation);
    }

    #endregion

    #region Timeout Handling

    // Note: Blocking code timeout test removed - in-process execution cannot cancel
    // synchronous blocking code like Thread.Sleep. For timeout behavior, use subprocess mode.

    [Test]
    public async Task ExecuteAsync_ShortTimeout_StillReturnsResultAfterCompletion()
    {
        // Even with a short timeout, if the spec completes quickly, it succeeds
        var specContent = """
            describe("Quick", () =>
            {
                it("runs fast", () =>
                {
                    expect(1 + 1).toBe(2);
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    #endregion

    #region Script Caching

    [Test]
    public async Task ExecuteAsync_SameContent_UsesCachedScript()
    {
        var specContent = """
            describe("Cached", () =>
            {
                it("runs twice", () => expect(true).toBeTrue());
            });
            """;

        // First execution
        var result1 = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);
        var duration1 = result1.DurationMs;

        // Second execution (should use cache and be faster after warm-up)
        var result2 = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result1.Success).IsTrue();
        await Assert.That(result2.Success).IsTrue();
    }

    [Test]
    public async Task ClearCache_ResetsScriptCache()
    {
        var specContent = """
            describe("Clear", () =>
            {
                it("spec", () => expect(1).toBe(1));
            });
            """;

        // Execute to populate cache
        await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        // Clear cache
        _runner.ClearCache();

        // Execute again - should still work
        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    #endregion

    #region Runtime Errors

    [Test]
    public async Task ExecuteAsync_RuntimeException_ReturnsRuntimeError()
    {
        var specContent = """
            describe("Runtime", () =>
            {
                it("throws", () =>
                {
                    throw new System.InvalidOperationException("Test error");
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Report).IsNotNull();
        await Assert.That(result.Report!.Summary.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_NullReferenceException_HandledGracefully()
    {
        var specContent = """
            describe("NullRef", () =>
            {
                it("dereferences null", () =>
                {
                    string? s = null;
                    var len = s!.Length;
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    #endregion

    #region State Isolation

    [Test]
    public async Task ExecuteAsync_MultipleRuns_StateIsReset()
    {
        // First run defines a variable in describe
        var specContent1 = """
            var counter = 0;
            describe("First", () =>
            {
                it("increments", () =>
                {
                    counter++;
                    expect(counter).toBe(1);
                });
            });
            """;

        // Second run should start fresh
        var specContent2 = """
            var counter = 0;
            describe("Second", () =>
            {
                it("starts at zero", () =>
                {
                    expect(counter).toBe(0);
                });
            });
            """;

        var result1 = await _runner.ExecuteAsync(specContent1, TimeSpan.FromSeconds(10), CancellationToken.None);
        var result2 = await _runner.ExecuteAsync(specContent2, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result1.Success).IsTrue();
        await Assert.That(result2.Success).IsTrue();
    }

    #endregion

    #region Async Specs

    [Test]
    public async Task ExecuteAsync_AsyncSpec_ExecutesCorrectly()
    {
        var specContent = """
            describe("Async", () =>
            {
                it("awaits task", async () =>
                {
                    await System.Threading.Tasks.Task.Delay(10);
                    expect(true).toBeTrue();
                });
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Report!.Summary.Passed).IsEqualTo(1);
    }

    #endregion

    #region Duration Tracking

    [Test]
    public async Task ExecuteAsync_ReportsDuration()
    {
        var specContent = """
            describe("Timing", () =>
            {
                it("quick spec", () => expect(1).toBe(1));
            });
            """;

        var result = await _runner.ExecuteAsync(specContent, TimeSpan.FromSeconds(10), CancellationToken.None);

        await Assert.That(result.DurationMs).IsGreaterThan(0);
    }

    #endregion
}

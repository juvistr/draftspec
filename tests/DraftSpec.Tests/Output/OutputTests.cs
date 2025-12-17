using System.Text.Json;
using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Output;

/// <summary>
/// Tests for spec output formatting (console and JSON).
/// These tests must run sequentially due to Console.SetOut manipulation.
/// </summary>
[NotInParallel]
public class OutputTests
{
    #region Console Output

    [Test]
    public async Task ConsoleOutput_PassingSpec_ShowsCheckmark()
    {
        var output = CaptureConsoleOutput(() =>
        {
            describe("test", () => { it("passes", () => { }); });
            run();
        });

        await Assert.That(output).Contains("✓");
        await Assert.That(output).Contains("passes");
    }

    [Test]
    public async Task ConsoleOutput_FailingSpec_ShowsX()
    {
        var output = CaptureConsoleOutput(() =>
        {
            describe("test", () => { it("fails", () => throw new Exception("error")); });
            run();
        });

        await Assert.That(output).Contains("✗");
        await Assert.That(output).Contains("fails");
    }

    [Test]
    public async Task ConsoleOutput_PendingSpec_ShowsCircle()
    {
        var output = CaptureConsoleOutput(() =>
        {
            describe("test", () => { it("pending"); });
            run();
        });

        await Assert.That(output).Contains("○");
        await Assert.That(output).Contains("pending");
    }

    [Test]
    public async Task ConsoleOutput_SkippedSpec_ShowsDash()
    {
        var output = CaptureConsoleOutput(() =>
        {
            describe("test", () => { xit("skipped", () => { }); });
            run();
        });

        await Assert.That(output).Contains("-");
        await Assert.That(output).Contains("skipped");
    }

    [Test]
    public async Task ConsoleOutput_ShowsContextDescription()
    {
        var output = CaptureConsoleOutput(() =>
        {
            describe("Calculator", () => { it("adds numbers", () => { }); });
            run();
        });

        await Assert.That(output).Contains("Calculator");
    }

    [Test]
    public async Task ConsoleOutput_ShowsNestedContexts()
    {
        var output = CaptureConsoleOutput(() =>
        {
            describe("Calculator", () => { describe("add method", () => { it("returns sum", () => { }); }); });
            run();
        });

        await Assert.That(output).Contains("Calculator");
        await Assert.That(output).Contains("add method");
    }

    [Test]
    public async Task ConsoleOutput_ShowsSummary()
    {
        var output = CaptureConsoleOutput(() =>
        {
            describe("test", () =>
            {
                it("passes", () => { });
                it("pending");
            });
            run();
        });

        await Assert.That(output).Contains("2 specs:");
        await Assert.That(output).Contains("passed");
        await Assert.That(output).Contains("pending");
    }

    [Test]
    public async Task ConsoleOutput_ShowsErrorMessage_OnFailure()
    {
        var output = CaptureConsoleOutput(() =>
        {
            describe("test",
                () => { it("fails", () => throw new InvalidOperationException("specific error message")); });
            run();
        });

        await Assert.That(output).Contains("specific error message");
    }

    [Test]
    public async Task ConsoleOutput_NoSpecs_ShowsMessage()
    {
        var output = CaptureConsoleOutput(() => { run(); });

        await Assert.That(output).Contains("No specs defined");
    }

    #endregion

    #region JSON Output

    [Test]
    public async Task JsonOutput_ContainsTimestamp()
    {
        var json = CaptureConsoleOutput(() =>
        {
            describe("test", () => { it("spec", () => { }); });
            run(true);
        });

        var doc = JsonDocument.Parse(json);
        await Assert.That(doc.RootElement.TryGetProperty("timestamp", out _)).IsTrue();
    }

    [Test]
    public async Task JsonOutput_ContainsSummary()
    {
        var json = CaptureConsoleOutput(() =>
        {
            describe("test", () =>
            {
                it("passes", () => { });
                it("pending");
            });
            run(true);
        });

        var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");

        await Assert.That(summary.GetProperty("total").GetInt32()).IsEqualTo(2);
        await Assert.That(summary.GetProperty("passed").GetInt32()).IsEqualTo(1);
        await Assert.That(summary.GetProperty("pending").GetInt32()).IsEqualTo(1);
    }

    [Test]
    public async Task JsonOutput_ContainsContexts()
    {
        var json = CaptureConsoleOutput(() =>
        {
            describe("Calculator", () => { it("works", () => { }); });
            run(true);
        });

        var doc = JsonDocument.Parse(json);
        var contexts = doc.RootElement.GetProperty("contexts");

        await Assert.That(contexts.GetArrayLength()).IsGreaterThan(0);
        await Assert.That(contexts[0].GetProperty("description").GetString()).IsEqualTo("Calculator");
    }

    [Test]
    public async Task JsonOutput_ContainsSpecDetails()
    {
        var json = CaptureConsoleOutput(() =>
        {
            describe("test", () => { it("spec description", () => { }); });
            run(true);
        });

        var doc = JsonDocument.Parse(json);
        var specs = doc.RootElement.GetProperty("contexts")[0].GetProperty("specs");

        await Assert.That(specs.GetArrayLength()).IsGreaterThan(0);
        await Assert.That(specs[0].GetProperty("description").GetString()).IsEqualTo("spec description");
        await Assert.That(specs[0].GetProperty("status").GetString()).IsEqualTo("passed");
    }

    [Test]
    public async Task JsonOutput_ContainsDuration()
    {
        var json = CaptureConsoleOutput(() =>
        {
            describe("test", () => { it("spec", () => Thread.Sleep(5)); });
            run(true);
        });

        var doc = JsonDocument.Parse(json);
        var spec = doc.RootElement.GetProperty("contexts")[0].GetProperty("specs")[0];

        await Assert.That(spec.TryGetProperty("durationMs", out var duration)).IsTrue();
        await Assert.That(duration.GetDouble()).IsGreaterThan(0);
    }

    [Test]
    public async Task JsonOutput_ContainsErrorMessage_OnFailure()
    {
        var json = CaptureConsoleOutput(() =>
        {
            describe("test", () => { it("fails", () => throw new Exception("json error")); });
            run(true);
        });

        var doc = JsonDocument.Parse(json);
        var spec = doc.RootElement.GetProperty("contexts")[0].GetProperty("specs")[0];

        await Assert.That(spec.GetProperty("status").GetString()).IsEqualTo("failed");
        await Assert.That(spec.GetProperty("error").GetString()).IsEqualTo("json error");
    }

    [Test]
    public async Task JsonOutput_NestedContexts_PreservesHierarchy()
    {
        var json = CaptureConsoleOutput(() =>
        {
            describe("outer", () => { describe("inner", () => { it("spec", () => { }); }); });
            run(true);
        });

        var doc = JsonDocument.Parse(json);
        var outer = doc.RootElement.GetProperty("contexts")[0];
        var inner = outer.GetProperty("contexts")[0];

        await Assert.That(outer.GetProperty("description").GetString()).IsEqualTo("outer");
        await Assert.That(inner.GetProperty("description").GetString()).IsEqualTo("inner");
    }

    [Test]
    public async Task JsonOutput_NoSpecs_ReturnsEmptyObject()
    {
        var json = CaptureConsoleOutput(() => { run(true); });

        await Assert.That(json.Trim()).IsEqualTo("{}");
    }

    #endregion

    #region Helper Methods

    private static string CaptureConsoleOutput(Action action)
    {
        var originalOut = Console.Out;
        var sw = new StringWriter();
        try
        {
            Console.SetOut(sw);
            action();
            sw.Flush();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
            sw.Dispose();
            // Reset exit code that may have been set
            Environment.ExitCode = 0;
        }
    }

    #endregion
}
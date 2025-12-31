using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Plugins;
using DraftSpec.Plugins.Reporters;

namespace DraftSpec.Tests.Reporters;

/// <summary>
/// Tests for StreamingConsoleReporter.
/// Tests run sequentially because they capture Console.Out.
/// </summary>
[NotInParallel]
public class StreamingConsoleReporterTests
{
    private TextWriter _originalOut = null!;
    private StringWriter _output = null!;

    [Before(Test)]
    public void SetUp()
    {
        _originalOut = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    [After(Test)]
    public void TearDown()
    {
        Console.SetOut(_originalOut);
        _output.Dispose();
    }

    #region Name

    [Test]
    public async Task Name_ReturnsStreamingConsole()
    {
        var reporter = new StreamingConsoleReporter();

        await Assert.That(reporter.Name).IsEqualTo("streaming-console");
    }

    #endregion

    #region OnRunStartingAsync

    [Test]
    public async Task OnRunStartingAsync_OutputsTotalSpecs()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var context = new RunStartingContext(42, DateTime.UtcNow);

        await reporter.OnRunStartingAsync(context);

        var output = _output.ToString();
        await Assert.That(output).Contains("Running 42 specs");
    }

    [Test]
    public async Task OnRunStartingAsync_OutputsNewlines()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var context = new RunStartingContext(10, DateTime.UtcNow);

        await reporter.OnRunStartingAsync(context);

        var output = _output.ToString();
        // Should have blank lines before and after the message
        await Assert.That(output).StartsWith(Environment.NewLine);
    }

    #endregion

    #region OnSpecCompletedAsync - Progress Symbols

    [Test]
    public async Task OnSpecCompletedAsync_PassedSpec_OutputsDot()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var result = CreateResult(SpecStatus.Passed);

        await reporter.OnSpecCompletedAsync(result);

        await Assert.That(_output.ToString()).IsEqualTo(".");
    }

    [Test]
    public async Task OnSpecCompletedAsync_FailedSpec_OutputsF()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var result = CreateResult(SpecStatus.Failed);

        await reporter.OnSpecCompletedAsync(result);

        await Assert.That(_output.ToString()).IsEqualTo("F");
    }

    [Test]
    public async Task OnSpecCompletedAsync_PendingSpec_OutputsP()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var result = CreateResult(SpecStatus.Pending);

        await reporter.OnSpecCompletedAsync(result);

        await Assert.That(_output.ToString()).IsEqualTo("P");
    }

    [Test]
    public async Task OnSpecCompletedAsync_SkippedSpec_OutputsDash()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var result = CreateResult(SpecStatus.Skipped);

        await reporter.OnSpecCompletedAsync(result);

        await Assert.That(_output.ToString()).IsEqualTo("-");
    }

    [Test]
    public async Task OnSpecCompletedAsync_MultipleSpecs_OutputsSymbolsInOrder()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);

        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Failed));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Pending));

        await Assert.That(_output.ToString()).IsEqualTo(".F.P");
    }

    #endregion

    #region OnSpecsBatchCompletedAsync

    [Test]
    public async Task OnSpecsBatchCompletedAsync_OutputsAllSymbols()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var results = new List<SpecResult>
        {
            CreateResult(SpecStatus.Passed),
            CreateResult(SpecStatus.Passed),
            CreateResult(SpecStatus.Failed),
            CreateResult(SpecStatus.Skipped)
        };

        await reporter.OnSpecsBatchCompletedAsync(results);

        await Assert.That(_output.ToString()).IsEqualTo("..F-");
    }

    [Test]
    public async Task OnSpecsBatchCompletedAsync_TracksFailures()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var failure = CreateResult(SpecStatus.Failed, "Test failure", new Exception("Something went wrong"));
        var results = new List<SpecResult> { CreateResult(SpecStatus.Passed), failure };

        await reporter.OnSpecsBatchCompletedAsync(results);
        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("Failures:");
        await Assert.That(output).Contains("Something went wrong");
    }

    #endregion

    #region OnRunCompletedAsync - Summary

    [Test]
    public async Task OnRunCompletedAsync_OutputsSummaryLine()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed, durationMs: 10));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed, durationMs: 20));

        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("2 specs:");
        await Assert.That(output).Contains("2 passed");
    }

    [Test]
    public async Task OnRunCompletedAsync_WithMixedResults_ShowsAllStatuses()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed, durationMs: 10));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Failed, durationMs: 10));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Pending, durationMs: 10));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Skipped, durationMs: 10));

        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("4 specs:");
        await Assert.That(output).Contains("1 passed");
        await Assert.That(output).Contains("1 failed");
        await Assert.That(output).Contains("1 pending");
        await Assert.That(output).Contains("1 skipped");
    }

    [Test]
    public async Task OnRunCompletedAsync_WithFailures_ShowsFailureDetails()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var failure = CreateResult(SpecStatus.Failed, "my failing spec", new Exception("Expected 1 but got 2"));

        await reporter.OnSpecCompletedAsync(failure);
        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("Failures:");
        await Assert.That(output).Contains("1)");
        await Assert.That(output).Contains("my failing spec");
        await Assert.That(output).Contains("Expected 1 but got 2");
    }

    [Test]
    public async Task OnRunCompletedAsync_WithMultipleFailures_NumbersThem()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Failed, "first failure", new Exception("Error 1")));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Failed, "second failure", new Exception("Error 2")));

        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        // FullDescription includes context path: "context first failure"
        await Assert.That(output).Contains("1) context first failure");
        await Assert.That(output).Contains("2) context second failure");
    }

    [Test]
    public async Task OnRunCompletedAsync_OutputsSeparatorLine()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed));

        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("--------------------------------------------------");
    }

    #endregion

    #region Duration Formatting

    [Test]
    public async Task Summary_Microseconds_FormatsCorrectly()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed, durationMs: 0.5)); // 0.5ms = 500µs

        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("µs");
    }

    [Test]
    public async Task Summary_Milliseconds_FormatsCorrectly()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed, durationMs: 50)); // 50ms

        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("50ms");
    }

    [Test]
    public async Task Summary_Seconds_FormatsCorrectly()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed, durationMs: 2500)); // 2.5s

        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("2.5s");
    }

    #endregion

    #region Color Output

    [Test]
    public async Task WithColors_DoesNotThrow()
    {
        var reporter = new StreamingConsoleReporter(useColors: true);

        await reporter.OnRunStartingAsync(new RunStartingContext(1, DateTime.UtcNow));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Failed));
        await reporter.OnRunCompletedAsync(CreateReport());

        // Just verify it completes without error
        await Assert.That(_output.ToString()).Contains("specs");
    }

    #endregion

    #region Thread Safety

    [Test]
    public async Task ConcurrentSpecs_NoRaceConditions()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var tasks = new List<Task>();

        // Simulate concurrent spec completions
        for (var i = 0; i < 100; i++)
        {
            var status = (i % 4) switch
            {
                0 => SpecStatus.Passed,
                1 => SpecStatus.Failed,
                2 => SpecStatus.Pending,
                _ => SpecStatus.Skipped
            };
            tasks.Add(reporter.OnSpecCompletedAsync(CreateResult(status, $"spec {i}")));
        }

        await Task.WhenAll(tasks);

        // Count symbols BEFORE summary (summary adds dashes in separator line)
        var progressOutput = _output.ToString();
        var symbolCount = progressOutput.Count(c => c == '.' || c == 'F' || c == 'P' || c == '-');
        await Assert.That(symbolCount).IsEqualTo(100);

        await reporter.OnRunCompletedAsync(CreateReport());

        var fullOutput = _output.ToString();
        // Should track 25 failures
        await Assert.That(fullOutput).Contains("25 failed");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task NoSpecs_OutputsZeroSummary()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);

        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("0 specs:");
    }

    [Test]
    public async Task FailureWithNullException_HandlesGracefully()
    {
        var reporter = new StreamingConsoleReporter(useColors: false);
        var spec = new SpecDefinition("test spec");
        var result = new SpecResult(spec, SpecStatus.Failed, ["context"], TimeSpan.FromMilliseconds(10), null);

        await reporter.OnSpecCompletedAsync(result);
        await reporter.OnRunCompletedAsync(CreateReport());

        var output = _output.ToString();
        await Assert.That(output).Contains("Unknown error");
    }

    #endregion

    #region Helper Methods

    private static SpecResult CreateResult(
        SpecStatus status,
        string description = "test spec",
        Exception? exception = null,
        double durationMs = 10)
    {
        var spec = new SpecDefinition(description);
        return new SpecResult(spec, status, ["context"], TimeSpan.FromMilliseconds(durationMs), exception);
    }

    private static SpecReport CreateReport()
    {
        return new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary(),
            Contexts = []
        };
    }

    #endregion
}

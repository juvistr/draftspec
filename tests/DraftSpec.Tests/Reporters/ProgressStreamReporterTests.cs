using System.Text.Json;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Plugins;
using DraftSpec.Plugins.Reporters;

namespace DraftSpec.Tests.Reporters;

/// <summary>
/// Tests for ProgressStreamReporter.
/// </summary>
[NotInParallel(nameof(Console))]
public class ProgressStreamReporterTests
{
    private StringWriter _output = null!;
    private TextWriter _originalOut = null!;

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

    [Test]
    public async Task OnRunStartingAsync_EmitsStartNotification()
    {
        var reporter = new ProgressStreamReporter();
        var context = new RunStartingContext(5, DateTime.UtcNow);

        await reporter.OnRunStartingAsync(context);

        var output = _output.ToString();
        await Assert.That(output).Contains("DRAFTSPEC_PROGRESS:");
        await Assert.That(output).Contains("\"type\": \"start\"");
        await Assert.That(output).Contains("\"total\": 5");
    }

    [Test]
    public async Task OnSpecCompletedAsync_EmitsProgressNotification()
    {
        var reporter = new ProgressStreamReporter();
        await reporter.OnRunStartingAsync(new RunStartingContext(3, DateTime.UtcNow));
        _output.GetStringBuilder().Clear();

        var result = CreateResult(SpecStatus.Passed, "test spec");

        await reporter.OnSpecCompletedAsync(result);

        var output = _output.ToString();
        await Assert.That(output).Contains("\"type\": \"progress\"");
        await Assert.That(output).Contains("\"status\": \"passed\"");
        await Assert.That(output).Contains("\"completed\": 1");
        await Assert.That(output).Contains("\"total\": 3");
    }

    [Test]
    public async Task OnSpecCompletedAsync_IncrementsCompleted()
    {
        var reporter = new ProgressStreamReporter();
        await reporter.OnRunStartingAsync(new RunStartingContext(3, DateTime.UtcNow));

        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed));
        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Failed));

        var output = _output.ToString();
        await Assert.That(output).Contains("\"completed\": 1");
        await Assert.That(output).Contains("\"completed\": 2");
    }

    [Test]
    public async Task OnSpecCompletedAsync_IncludesSpecDescription()
    {
        var reporter = new ProgressStreamReporter();
        await reporter.OnRunStartingAsync(new RunStartingContext(1, DateTime.UtcNow));
        _output.GetStringBuilder().Clear();

        await reporter.OnSpecCompletedAsync(CreateResult(SpecStatus.Passed, "my test description"));

        var output = _output.ToString();
        await Assert.That(output).Contains("my test description");
    }

    [Test]
    public async Task OnRunCompletedAsync_EmitsCompleteNotification()
    {
        var reporter = new ProgressStreamReporter();
        await reporter.OnRunStartingAsync(new RunStartingContext(5, DateTime.UtcNow));
        _output.GetStringBuilder().Clear();

        var report = new SpecReport
        {
            Summary = new SpecSummary
            {
                Total = 5,
                Passed = 3,
                Failed = 1,
                Pending = 1,
                Skipped = 0,
                DurationMs = 100
            }
        };

        await reporter.OnRunCompletedAsync(report);

        var output = _output.ToString();
        await Assert.That(output).Contains("\"type\": \"complete\"");
        await Assert.That(output).Contains("\"passed\": 3");
        await Assert.That(output).Contains("\"failed\": 1");
        await Assert.That(output).Contains("\"pending\": 1");
    }

    [Test]
    public async Task Name_ReturnsProgressStream()
    {
        var reporter = new ProgressStreamReporter();

        await Assert.That(reporter.Name).IsEqualTo("progress-stream");
    }

    [Test]
    public async Task OutputFormat_HasProgressPrefix()
    {
        var reporter = new ProgressStreamReporter();
        await reporter.OnRunStartingAsync(new RunStartingContext(1, DateTime.UtcNow));

        var output = _output.ToString();
        // Each progress notification should start with the prefix on a new line
        var progressLines = output.Split(Environment.NewLine)
            .Where(l => l.StartsWith("DRAFTSPEC_PROGRESS:"))
            .ToList();

        await Assert.That(progressLines.Count).IsGreaterThanOrEqualTo(1);
        // The content after the prefix should start valid JSON
        await Assert.That(progressLines[0]).Contains("{");
    }

    [Test]
    public async Task OnSpecsBatchCompletedAsync_EmitsProgressForEachSpec()
    {
        var reporter = new ProgressStreamReporter();
        await reporter.OnRunStartingAsync(new RunStartingContext(3, DateTime.UtcNow));
        _output.GetStringBuilder().Clear();

        var results = new List<SpecResult>
        {
            CreateResult(SpecStatus.Passed, "spec1"),
            CreateResult(SpecStatus.Failed, "spec2"),
            CreateResult(SpecStatus.Pending, "spec3")
        };

        await reporter.OnSpecsBatchCompletedAsync(results);

        var output = _output.ToString();
        await Assert.That(output).Contains("\"completed\": 1");
        await Assert.That(output).Contains("\"completed\": 2");
        await Assert.That(output).Contains("\"completed\": 3");
    }

    private static SpecResult CreateResult(SpecStatus status, string description = "spec")
    {
        var spec = new SpecDefinition(description);
        return new SpecResult(spec, status, ["context"], TimeSpan.FromMilliseconds(10), null);
    }
}

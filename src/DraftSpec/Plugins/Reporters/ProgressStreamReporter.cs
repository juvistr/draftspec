using System.Text.Json;
using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// A reporter that streams JSON progress lines to stdout for MCP integration.
/// Each spec completion emits a JSON line that can be parsed in real-time.
/// </summary>
/// <remarks>
/// Output format (one JSON object per line):
/// {"type":"progress","spec":"description","status":"passed","completed":1,"total":10,"durationMs":5.2}
/// </remarks>
public class ProgressStreamReporter : IReporter
{
    /// <summary>
    /// Prefix for progress lines to distinguish from other output.
    /// </summary>
    public const string ProgressLinePrefix = "DRAFTSPEC_PROGRESS:";

    private readonly object _lock = new();
    private int _completed;
    private int _totalSpecs;

    /// <inheritdoc />
    public string Name => "progress-stream";

    /// <inheritdoc />
    public Task OnRunStartingAsync(RunStartingContext context)
    {
        _totalSpecs = context.TotalSpecs;
        _completed = 0;

        // Emit start notification
        WriteProgressLine(new ProgressLine
        {
            Type = "start",
            Total = context.TotalSpecs
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnSpecCompletedAsync(SpecResult result)
    {
        int completed;
        lock (_lock)
        {
            completed = ++_completed;
        }

        WriteProgressLine(new ProgressLine
        {
            Type = "progress",
            Spec = result.FullDescription,
            Status = result.Status.ToString().ToLowerInvariant(),
            Completed = completed,
            Total = _totalSpecs,
            DurationMs = result.TotalDuration.TotalMilliseconds
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnSpecsBatchCompletedAsync(IReadOnlyList<SpecResult> results)
    {
        foreach (var result in results)
        {
            OnSpecCompletedAsync(result);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnRunCompletedAsync(SpecReport report)
    {
        WriteProgressLine(new ProgressLine
        {
            Type = "complete",
            Completed = report.Summary.Total,
            Total = report.Summary.Total,
            Passed = report.Summary.Passed,
            Failed = report.Summary.Failed,
            Pending = report.Summary.Pending,
            Skipped = report.Summary.Skipped,
            DurationMs = report.Summary.DurationMs
        });

        return Task.CompletedTask;
    }

    private void WriteProgressLine(ProgressLine line)
    {
        var json = JsonSerializer.Serialize(line, JsonOptionsProvider.Default);
        lock (_lock)
        {
            Console.WriteLine($"{ProgressLinePrefix}{json}");
        }
    }

    /// <summary>
    /// Progress line data structure.
    /// </summary>
    private class ProgressLine
    {
        public string Type { get; init; } = "";
        public string? Spec { get; init; }
        public string? Status { get; init; }
        public int Completed { get; init; }
        public int Total { get; init; }
        public int? Passed { get; init; }
        public int? Failed { get; init; }
        public int? Pending { get; init; }
        public int? Skipped { get; init; }
        public double DurationMs { get; init; }
    }
}

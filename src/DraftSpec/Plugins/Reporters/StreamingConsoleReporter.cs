using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// A console reporter that streams results as specs complete.
/// Outputs dots/symbols for each spec and summary at the end.
/// </summary>
/// <remarks>
/// Unlike <see cref="ConsoleReporter"/>, this reporter doesn't need the full
/// SpecReport tree. It tracks statistics progressively via <see cref="StreamingStats"/>.
///
/// This is useful for large test suites where you want immediate feedback
/// without accumulating all results in memory before output.
/// </remarks>
public class StreamingConsoleReporter : IReporter
{
    private readonly StreamingStats _stats = new();
    private readonly List<SpecResult> _failures = [];
    private readonly bool _useColors;
    private readonly object _lock = new();
    private int _totalSpecs;

    /// <summary>
    /// Creates a streaming console reporter.
    /// </summary>
    /// <param name="useColors">Whether to use ANSI colors in output</param>
    public StreamingConsoleReporter(bool useColors = true)
    {
        _useColors = useColors;
    }

    /// <inheritdoc />
    public string Name => "streaming-console";

    /// <inheritdoc />
    public Task OnRunStartingAsync(RunStartingContext context)
    {
        _totalSpecs = context.TotalSpecs;
        Console.WriteLine();
        Console.WriteLine($"Running {context.TotalSpecs} specs...");
        Console.WriteLine();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnSpecCompletedAsync(SpecResult result)
    {
        _stats.Add(result);

        // Track failures for final summary
        if (result.Status == SpecStatus.Failed)
            lock (_failures)
            {
                _failures.Add(result);
            }

        // Output progress symbol
        var (symbol, color) = result.Status switch
        {
            SpecStatus.Passed => (".", ConsoleColor.Green),
            SpecStatus.Failed => ("F", ConsoleColor.Red),
            SpecStatus.Pending => ("P", ConsoleColor.Yellow),
            SpecStatus.Skipped => ("-", ConsoleColor.DarkGray),
            _ => ("?", ConsoleColor.White)
        };

        lock (_lock)
        {
            if (_useColors) Console.ForegroundColor = color;
            Console.Write(symbol);
            if (_useColors) Console.ResetColor();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnSpecsBatchCompletedAsync(IReadOnlyList<SpecResult> results)
    {
        foreach (var result in results)
        {
            _stats.Add(result);

            if (result.Status == SpecStatus.Failed)
                lock (_failures)
                {
                    _failures.Add(result);
                }
        }

        // Output batch symbols at once
        lock (_lock)
        {
            foreach (var result in results)
            {
                var (symbol, color) = result.Status switch
                {
                    SpecStatus.Passed => (".", ConsoleColor.Green),
                    SpecStatus.Failed => ("F", ConsoleColor.Red),
                    SpecStatus.Pending => ("P", ConsoleColor.Yellow),
                    SpecStatus.Skipped => ("-", ConsoleColor.DarkGray),
                    _ => ("?", ConsoleColor.White)
                };

                if (_useColors) Console.ForegroundColor = color;
                Console.Write(symbol);
                if (_useColors) Console.ResetColor();
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnRunCompletedAsync(SpecReport report)
    {
        // Use our accumulated stats, not the report
        WriteSummary();
        return Task.CompletedTask;
    }

    private void WriteSummary()
    {
        Console.WriteLine();
        Console.WriteLine();

        // Write failure details
        if (_failures.Count > 0)
        {
            Console.WriteLine("Failures:");
            Console.WriteLine();

            for (var i = 0; i < _failures.Count; i++)
            {
                var failure = _failures[i];
                Console.WriteLine($"  {i + 1}) {failure.FullDescription}");

                if (_useColors) Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"     {failure.Exception?.Message ?? "Unknown error"}");
                if (_useColors) Console.ResetColor();

                Console.WriteLine();
            }
        }

        // Write summary line
        Console.WriteLine(new string('-', 50));
        Console.Write($"{_stats.Total} specs: ");

        var first = true;

        void WriteStat(int count, string label, ConsoleColor color)
        {
            if (count == 0) return;
            if (!first) Console.Write(", ");
            first = false;

            if (_useColors) Console.ForegroundColor = color;
            Console.Write($"{count} {label}");
            if (_useColors) Console.ResetColor();
        }

        WriteStat(_stats.Passed, "passed", ConsoleColor.Green);
        WriteStat(_stats.Failed, "failed", ConsoleColor.Red);
        WriteStat(_stats.Pending, "pending", ConsoleColor.Yellow);
        WriteStat(_stats.Skipped, "skipped", ConsoleColor.DarkGray);

        if (_useColors) Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($" in {FormatDuration(_stats.TotalDurationMs)}");
        if (_useColors) Console.ResetColor();
        Console.WriteLine();
    }

    private static string FormatDuration(double ms)
    {
        return ms switch
        {
            < 1 => $"{ms * 1000:F0}µs",
            < 1000 => $"{ms:F0}ms",
            _ => $"{ms / 1000:F1}s"
        };
    }
}
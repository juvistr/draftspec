namespace DraftSpec.Formatters.Console;

/// <summary>
/// Formats spec reports as colored console output.
/// </summary>
public class ConsoleFormatter : IConsoleFormatter
{
    /// <summary>
    /// Format and write a spec report to the provided TextWriter.
    /// </summary>
    public void Format(SpecReport report, TextWriter output, bool useColors = true)
    {
        output.WriteLine();

        foreach (var context in report.Contexts)
        {
            FormatContext(context, output, useColors, level: 0);
        }

        // Summary
        output.WriteLine();
        output.WriteLine(new string('-', 50));

        output.Write($"{report.Summary.Total} specs: ");

        var first = true;
        void WriteStat(int count, string label, ConsoleColor color)
        {
            if (count == 0) return;
            if (!first) output.Write(", ");
            first = false;

            if (useColors) System.Console.ForegroundColor = color;
            output.Write($"{count} {label}");
            if (useColors) System.Console.ResetColor();
        }

        WriteStat(report.Summary.Passed, "passed", ConsoleColor.Green);
        WriteStat(report.Summary.Failed, "failed", ConsoleColor.Red);
        WriteStat(report.Summary.Pending, "pending", ConsoleColor.Yellow);
        WriteStat(report.Summary.Skipped, "skipped", ConsoleColor.DarkGray);

        if (useColors) System.Console.ForegroundColor = ConsoleColor.DarkGray;
        output.Write($" in {FormatDuration(report.Summary.DurationMs)}");
        if (useColors) System.Console.ResetColor();
        output.WriteLine();
    }

    private void FormatContext(SpecContextReport context, TextWriter output, bool useColors, int level)
    {
        var indent = new string(' ', level * 2);

        // Print context description
        output.WriteLine($"{indent}{context.Description}");

        // Print specs in this context
        var specIndent = new string(' ', (level + 1) * 2);
        foreach (var spec in context.Specs)
        {
            var (symbol, color) = spec.Status switch
            {
                "passed" => ("✓", ConsoleColor.Green),
                "failed" => ("✗", ConsoleColor.Red),
                "pending" => ("○", ConsoleColor.Yellow),
                "skipped" => ("-", ConsoleColor.DarkGray),
                _ => ("?", ConsoleColor.White)
            };

            if (useColors) System.Console.ForegroundColor = color;
            output.Write($"{specIndent}{symbol} ");
            if (useColors) System.Console.ResetColor();
            output.Write(spec.Description);

            // Show duration for specs that ran
            if (spec.Status is "passed" or "failed" && spec.DurationMs > 0)
            {
                if (useColors) System.Console.ForegroundColor = ConsoleColor.DarkGray;
                output.Write($" ({FormatDuration(spec.DurationMs ?? 0)})");
                if (useColors) System.Console.ResetColor();
            }
            output.WriteLine();

            // Show error for failed specs
            if (spec.Failed && !string.IsNullOrEmpty(spec.Error))
            {
                if (useColors) System.Console.ForegroundColor = ConsoleColor.Red;
                output.WriteLine($"{specIndent}  {spec.Error}");
                if (useColors) System.Console.ResetColor();
            }
        }

        // Recurse into nested contexts
        foreach (var child in context.Contexts)
        {
            FormatContext(child, output, useColors, level + 1);
        }
    }

    private static string FormatDuration(double durationMs)
    {
        if (durationMs < 1)
            return $"{durationMs * 1000:F0}µs";
        if (durationMs < 1000)
            return $"{durationMs:F0}ms";
        return $"{durationMs / 1000:F2}s";
    }
}

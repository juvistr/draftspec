using DraftSpec.Formatters;

namespace DraftSpec.Formatters.Console;

/// <summary>
/// Formats spec reports as colored console output.
/// </summary>
public class ConsoleFormatter : IConsoleFormatter
{
    /// <summary>
    /// Console output doesn't have a typical file extension.
    /// </summary>
    public string FileExtension => ".txt";

    /// <summary>
    /// Format a spec report to a string (without colors).
    /// </summary>
    public string Format(SpecReport report)
    {
        using var writer = new StringWriter();
        Format(report, writer, useColors: false);
        return writer.ToString();
    }

    /// <summary>
    /// Format and write a spec report to the provided TextWriter (with colors by default).
    /// </summary>
    public void Format(SpecReport report, TextWriter output)
    {
        Format(report, output, useColors: true);
    }

    /// <summary>
    /// Format and write a spec report to the provided TextWriter with color control.
    /// </summary>
    public void Format(SpecReport report, TextWriter output, bool useColors)
    {
        output.WriteLine();

        foreach (var context in report.Contexts) FormatContext(context, output, useColors, 0);

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
                SpecStatusNames.Passed => ("✓", ConsoleColor.Green),
                SpecStatusNames.Failed => ("✗", ConsoleColor.Red),
                SpecStatusNames.Pending => ("○", ConsoleColor.Yellow),
                SpecStatusNames.Skipped => ("-", ConsoleColor.DarkGray),
                _ => ("?", ConsoleColor.White)
            };

            if (useColors) System.Console.ForegroundColor = color;
            output.Write($"{specIndent}{symbol} ");
            if (useColors) System.Console.ResetColor();
            output.Write(spec.Description);

            // Show duration for specs that ran
            if (spec.Status is SpecStatusNames.Passed or SpecStatusNames.Failed && spec.DurationMs > 0)
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
        foreach (var child in context.Contexts) FormatContext(child, output, useColors, level + 1);
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
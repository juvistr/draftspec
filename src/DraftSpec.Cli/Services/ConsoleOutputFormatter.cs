using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Formats spec reports for console output.
/// </summary>
public static class ConsoleOutputFormatter
{
    /// <summary>
    /// Format a spec report for console display with tree structure.
    /// </summary>
    public static string Format(SpecReport report)
    {
        var lines = new List<string>();

        void FormatContext(SpecContextReport ctx, int indent)
        {
            var prefix = new string(' ', indent * 2);
            lines.Add($"{prefix}{ctx.Description}");

            foreach (var spec in ctx.Specs)
            {
                var status = spec.Status switch
                {
                    "passed" => "\u2713",
                    "failed" => "\u2717",
                    "pending" => "\u25cb",
                    "skipped" => "-",
                    _ => "?"
                };
                lines.Add($"{prefix}  {status} {spec.Description}");
                if (!string.IsNullOrEmpty(spec.Error))
                {
                    lines.Add($"{prefix}    {spec.Error}");
                }
            }

            foreach (var child in ctx.Contexts)
            {
                FormatContext(child, indent + 1);
            }
        }

        foreach (var ctx in report.Contexts)
        {
            FormatContext(ctx, 0);
        }

        return string.Join(Environment.NewLine, lines);
    }
}

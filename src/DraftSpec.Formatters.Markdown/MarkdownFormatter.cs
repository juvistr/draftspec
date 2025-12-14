using System.Text;

namespace DraftSpec.Formatters.Markdown;

/// <summary>
/// Formats spec reports as Markdown.
/// </summary>
public class MarkdownFormatter : IFormatter
{
    public string FileExtension => ".md";

    public string Format(SpecReport report)
    {
        var sb = new StringBuilder();

        foreach (var context in report.Contexts)
        {
            FormatContext(sb, context, level: 1);
        }

        // Summary
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append($"**{report.Summary.Total} specs**: ");

        var parts = new List<string>();
        if (report.Summary.Passed > 0) parts.Add($"{report.Summary.Passed} passed");
        if (report.Summary.Failed > 0) parts.Add($"{report.Summary.Failed} failed");
        if (report.Summary.Pending > 0) parts.Add($"{report.Summary.Pending} pending");
        if (report.Summary.Skipped > 0) parts.Add($"{report.Summary.Skipped} skipped");
        sb.AppendLine(string.Join(", ", parts));

        sb.AppendLine();
        var footer = new List<string> { $"Generated {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC" };
        if (!string.IsNullOrEmpty(report.Source))
            footer.Add($"from `{report.Source}`");
        sb.AppendLine($"*{string.Join(" ", footer)}*");

        return sb.ToString();
    }

    private void FormatContext(StringBuilder sb, SpecContextReport context, int level)
    {
        // Context heading
        var heading = new string('#', Math.Min(level, 6));
        sb.AppendLine($"{heading} {context.Description}");
        sb.AppendLine();

        // Specs as list items
        foreach (var spec in context.Specs)
        {
            if (spec.Skipped)
            {
                sb.AppendLine($"- ~~{spec.Description}~~ *(skipped)*");
            }
            else
            {
                var symbol = spec.Status switch
                {
                    "passed" => "✓",
                    "failed" => "✗",
                    "pending" => "○",
                    _ => "?"
                };
                sb.AppendLine($"- {symbol} {spec.Description}");
            }

            if (spec.Failed && !string.IsNullOrEmpty(spec.Error))
            {
                sb.AppendLine($"  > {spec.Error}");
            }
        }

        if (context.Specs.Count > 0)
        {
            sb.AppendLine();
        }

        // Nested contexts
        foreach (var child in context.Contexts)
        {
            FormatContext(sb, child, level + 1);
        }
    }
}

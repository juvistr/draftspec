using System.Text;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Formatters.Markdown;

/// <summary>
/// Formats spec reports as Markdown with nested headings and list items.
/// </summary>
/// <remarks>
/// Uses GitHub-flavored Markdown syntax with checkmarks, strikethrough for skipped specs,
/// and blockquotes for error messages.
/// </remarks>
public class MarkdownFormatter : IFormatter
{
    /// <summary>
    /// The file extension for Markdown output.
    /// </summary>
    public string FileExtension => ".md";

    /// <summary>
    /// Formats the spec report as a Markdown document.
    /// </summary>
    /// <param name="report">The spec report to format.</param>
    /// <returns>A Markdown-formatted string.</returns>
    public string Format(SpecReport report)
    {
        var sb = new StringBuilder();

        foreach (var context in report.Contexts) FormatContext(sb, context, 1);

        // Summary
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append($"**{report.Summary.Total} specs**: ");

        // Inline stats without intermediate List allocation
        var first = true;
        if (report.Summary.Passed > 0)
        {
            sb.Append($"{report.Summary.Passed} passed");
            first = false;
        }

        if (report.Summary.Failed > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"{report.Summary.Failed} failed");
            first = false;
        }

        if (report.Summary.Pending > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"{report.Summary.Pending} pending");
            first = false;
        }

        if (report.Summary.Skipped > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"{report.Summary.Skipped} skipped");
        }

        sb.AppendLine();

        sb.AppendLine();
        sb.Append($"*Generated {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        if (!string.IsNullOrEmpty(report.Source))
            sb.Append($" from `{report.Source}`");
        sb.AppendLine("*");

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
                    SpecStatusNames.Passed => "✓",
                    SpecStatusNames.Failed => "✗",
                    SpecStatusNames.Pending => "○",
                    _ => "?"
                };
                sb.AppendLine($"- {symbol} {spec.Description}");
            }

            if (spec.Failed && !string.IsNullOrEmpty(spec.Error)) sb.AppendLine($"  > {spec.Error}");
        }

        if (context.Specs.Count > 0) sb.AppendLine();

        // Nested contexts
        foreach (var child in context.Contexts) FormatContext(sb, child, level + 1);
    }
}
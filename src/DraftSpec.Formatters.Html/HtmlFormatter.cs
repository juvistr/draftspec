using System.Text;
using System.Web;

namespace DraftSpec.Formatters.Html;

/// <summary>
/// Formats spec reports as HTML with CDN-based styling.
/// </summary>
public class HtmlFormatter : IFormatter
{
    private readonly HtmlOptions _options;

    public HtmlFormatter() : this(new HtmlOptions()) { }

    public HtmlFormatter(HtmlOptions options)
    {
        _options = options;
    }

    public string FileExtension => ".html";

    public string Format(SpecReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{Escape(_options.Title)}</title>");
        sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{Escape(_options.CssUrl)}\">");
        sb.AppendLine("  <style>");
        sb.AppendLine("    .passed { color: #22c55e; }");
        sb.AppendLine("    .failed { color: #ef4444; }");
        sb.AppendLine("    .pending { color: #eab308; }");
        sb.AppendLine("    .skipped { color: #6b7280; }");
        sb.AppendLine("    .error { background: #fef2f2; padding: 0.5rem; border-left: 3px solid #ef4444; margin: 0.5rem 0; }");
        sb.AppendLine("    .summary { margin-top: 2rem; padding-top: 1rem; border-top: 1px solid #e5e7eb; }");
        sb.AppendLine("    ul { list-style: none; padding-left: 1rem; }");
        if (!string.IsNullOrEmpty(_options.CustomCss))
        {
            sb.AppendLine(_options.CustomCss);
        }
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        foreach (var context in report.Contexts)
        {
            FormatContext(sb, context, level: 1);
        }

        // Summary
        sb.AppendLine("  <div class=\"summary\">");
        sb.AppendLine($"    <p><strong>{report.Summary.Total} specs</strong>: ");

        var parts = new List<string>();
        if (report.Summary.Passed > 0) parts.Add($"<span class=\"passed\">{report.Summary.Passed} passed</span>");
        if (report.Summary.Failed > 0) parts.Add($"<span class=\"failed\">{report.Summary.Failed} failed</span>");
        if (report.Summary.Pending > 0) parts.Add($"<span class=\"pending\">{report.Summary.Pending} pending</span>");
        if (report.Summary.Skipped > 0) parts.Add($"<span class=\"skipped\">{report.Summary.Skipped} skipped</span>");
        sb.AppendLine(string.Join(", ", parts));

        sb.AppendLine("    </p>");
        var footerParts = new List<string> { $"Generated {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC" };
        if (!string.IsNullOrEmpty(report.Source))
            footerParts.Add($"from <code>{Escape(report.Source)}</code>");
        sb.AppendLine($"    <p><small>{string.Join(" ", footerParts)}</small></p>");
        sb.AppendLine("  </div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private void FormatContext(StringBuilder sb, SpecContextReport context, int level)
    {
        var tag = level switch
        {
            1 => "h1",
            2 => "h2",
            3 => "h3",
            4 => "h4",
            5 => "h5",
            _ => "h6"
        };

        sb.AppendLine($"  <{tag}>{Escape(context.Description)}</{tag}>");

        if (context.Specs.Count > 0)
        {
            sb.AppendLine("  <ul>");
            foreach (var spec in context.Specs)
            {
                var symbol = spec.Status switch
                {
                    "passed" => "✓",
                    "failed" => "✗",
                    "pending" => "○",
                    "skipped" => "−",
                    _ => "?"
                };

                if (spec.Failed && !string.IsNullOrEmpty(spec.Error))
                {
                    sb.AppendLine($"    <li class=\"{spec.Status}\">{symbol} {Escape(spec.Description)}");
                    sb.AppendLine($"      <div class=\"error\"><code>{Escape(spec.Error)}</code></div>");
                    sb.AppendLine($"    </li>");
                }
                else
                {
                    sb.AppendLine($"    <li class=\"{spec.Status}\">{symbol} {Escape(spec.Description)}</li>");
                }
            }
            sb.AppendLine("  </ul>");
        }

        foreach (var child in context.Contexts)
        {
            FormatContext(sb, child, level + 1);
        }
    }

    private static string Escape(string text) => HttpUtility.HtmlEncode(text);
}

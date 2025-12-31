using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Formatters.Html;

/// <summary>
/// Formats spec reports as HTML with CDN-based styling.
/// </summary>
/// <remarks>
/// Uses Simple.css by default for minimal, clean styling. Custom CSS can be
/// injected via <see cref="HtmlOptions.CustomCss"/> (sanitized to prevent XSS).
/// </remarks>
public class HtmlFormatter : IFormatter
{
    private readonly HtmlOptions _options;

    /// <summary>
    /// Creates an HTML formatter with default options.
    /// </summary>
    public HtmlFormatter() : this(new HtmlOptions())
    {
    }

    /// <summary>
    /// Creates an HTML formatter with custom options.
    /// </summary>
    /// <param name="options">Formatting options including CSS customization.</param>
    public HtmlFormatter(HtmlOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// The file extension for HTML output.
    /// </summary>
    public string FileExtension => ".html";

    /// <summary>
    /// Formats the spec report as an HTML document.
    /// </summary>
    /// <param name="report">The spec report to format.</param>
    /// <returns>A complete HTML document string.</returns>
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
        sb.AppendLine(
            "    .error { background: #fef2f2; padding: 0.5rem; border-left: 3px solid #ef4444; margin: 0.5rem 0; }");
        sb.AppendLine("    .summary { margin-top: 2rem; padding-top: 1rem; border-top: 1px solid #e5e7eb; }");
        sb.AppendLine("    ul { list-style: none; padding-left: 1rem; }");
        if (!string.IsNullOrEmpty(_options.CustomCss)) sb.AppendLine(SanitizeCss(_options.CustomCss));
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        foreach (var context in report.Contexts) FormatContext(sb, context, 1);

        // Summary
        sb.AppendLine("  <div class=\"summary\">");
        sb.Append($"    <p><strong>{report.Summary.Total} specs</strong>: ");

        // Inline stats without intermediate List allocation
        var first = true;
        if (report.Summary.Passed > 0)
        {
            sb.Append($"<span class=\"passed\">{report.Summary.Passed} passed</span>");
            first = false;
        }

        if (report.Summary.Failed > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"<span class=\"failed\">{report.Summary.Failed} failed</span>");
            first = false;
        }

        if (report.Summary.Pending > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"<span class=\"pending\">{report.Summary.Pending} pending</span>");
            first = false;
        }

        if (report.Summary.Skipped > 0)
        {
            if (!first) sb.Append(", ");
            sb.Append($"<span class=\"skipped\">{report.Summary.Skipped} skipped</span>");
        }

        sb.AppendLine();

        sb.AppendLine("    </p>");
        sb.Append($"    <p><small>Generated {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        if (!string.IsNullOrEmpty(report.Source))
            sb.Append($" from <code>{Escape(report.Source)}</code>");
        sb.AppendLine("</small></p>");
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
                    SpecStatusNames.Passed => "✓",
                    SpecStatusNames.Failed => "✗",
                    SpecStatusNames.Pending => "○",
                    SpecStatusNames.Skipped => "−",
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

        foreach (var child in context.Contexts) FormatContext(sb, child, level + 1);
    }

    private static string Escape(string text)
    {
        return HttpUtility.HtmlEncode(text);
    }

    /// <summary>
    /// Timeout for CSS sanitization regex operations to prevent ReDoS.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Pattern to match HTML tags that could break out of the style element.
    /// Matches &lt;/ followed by style, script, link, or import (with optional whitespace).
    /// </summary>
    private static readonly Regex HtmlTagPattern = new(
        @"<\s*/?\s*(style|script|link|import)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        RegexTimeout);

    /// <summary>
    /// Pattern to match dangerous CSS functions and properties.
    /// Includes expression(), behavior:, -moz-binding, and @import.
    /// </summary>
    private static readonly Regex DangerousCssPattern = new(
        @"expression\s*\(|behavior\s*:|@import|-moz-binding",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        RegexTimeout);

    /// <summary>
    /// Pattern to match url() with dangerous protocols.
    /// Matches url( followed by optional quotes/whitespace then javascript:, data:, or vbscript:.
    /// </summary>
    private static readonly Regex DangerousUrlPattern = new(
        @"url\s*\(\s*['""]?\s*(javascript|data|vbscript)\s*:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        RegexTimeout);

    /// <summary>
    /// Sanitize CSS to prevent XSS via style tag escape and browser-specific CSS attacks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This sanitizer uses a blocklist approach to remove known dangerous patterns.
    /// It is designed for developer-provided CSS in trusted environments (e.g., test reports).
    /// </para>
    /// <para>
    /// For untrusted user input, consider using a proper CSS parser or whitelist approach.
    /// </para>
    /// <para>
    /// Blocked patterns include:
    /// <list type="bullet">
    ///   <item>HTML tags that could break out of the style element</item>
    ///   <item>expression() - IE JavaScript execution</item>
    ///   <item>behavior: - IE behavior binding</item>
    ///   <item>-moz-binding - Firefox XBL binding</item>
    ///   <item>@import - external stylesheet loading</item>
    ///   <item>url() with javascript:, data:, or vbscript: protocols</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static string SanitizeCss(string css)
    {
        // Remove HTML tags that could break out of the style element
        var result = HtmlTagPattern.Replace(css, "/* removed */");

        // Remove dangerous CSS functions and properties
        result = DangerousCssPattern.Replace(result, "/* removed */");

        // Remove dangerous URL protocols
        result = DangerousUrlPattern.Replace(result, "url(/* removed */");

        return result;
    }
}
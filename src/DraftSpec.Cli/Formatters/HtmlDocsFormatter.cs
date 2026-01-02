using System.Text;
using System.Web;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats specs as HTML documentation with collapsible sections.
/// </summary>
public sealed class HtmlDocsFormatter : IDocsFormatter
{
    public string Format(IReadOnlyList<DiscoveredSpec> specs, DocsMetadata metadata)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>Test Specifications</title>");
        sb.AppendLine("  <link rel=\"stylesheet\" href=\"https://cdn.simplecss.org/simple.min.css\">");
        AppendStyles(sb);
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <header>");
        sb.AppendLine("    <h1>Test Specifications</h1>");
        sb.AppendLine("  </header>");
        sb.AppendLine("  <main>");

        // Build tree structure
        var tree = BuildTree(specs);

        // Render tree as HTML
        foreach (var child in tree.Children)
        {
            RenderContext(child, sb, metadata.Results);
        }

        // Render any root-level specs
        RenderSpecs(tree.Specs, sb, metadata.Results);

        // Summary
        sb.AppendLine("    <hr>");
        sb.AppendLine("    <div class=\"summary\">");
        sb.Append($"      <p><strong>{specs.Count} specs</strong>");
        AppendSummaryStats(sb, specs, metadata.Results);
        sb.AppendLine("</p>");
        sb.AppendLine($"      <p><small>Generated {Escape(metadata.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"))} UTC");
        if (!string.IsNullOrEmpty(metadata.Source))
            sb.Append($" from <code>{Escape(metadata.Source)}</code>");
        sb.AppendLine("</small></p>");
        sb.AppendLine("    </div>");

        sb.AppendLine("  </main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void AppendStyles(StringBuilder sb)
    {
        sb.AppendLine("  <style>");
        sb.AppendLine("    .passed { color: #22c55e; }");
        sb.AppendLine("    .failed { color: #ef4444; }");
        sb.AppendLine("    .pending { color: #eab308; }");
        sb.AppendLine("    .skipped { color: #6b7280; text-decoration: line-through; }");
        sb.AppendLine("    .spec-list { list-style: none; padding-left: 0; }");
        sb.AppendLine("    .spec-list li { padding: 0.25rem 0; }");
        sb.AppendLine("    .spec-list li::before { content: ''; margin-right: 0.5rem; }");
        sb.AppendLine("    .badge { font-size: 0.75rem; padding: 0.125rem 0.375rem; border-radius: 0.25rem; margin-left: 0.5rem; }");
        sb.AppendLine("    .badge-pending { background: #fef3c7; color: #92400e; }");
        sb.AppendLine("    .badge-skipped { background: #f3f4f6; color: #4b5563; }");
        sb.AppendLine("    .badge-focused { background: #dbeafe; color: #1e40af; }");
        sb.AppendLine("    .badge-failed { background: #fee2e2; color: #991b1b; }");
        sb.AppendLine("    details { margin: 1rem 0; }");
        sb.AppendLine("    details summary { cursor: pointer; font-weight: 600; padding: 0.5rem; background: #f9fafb; border-radius: 0.25rem; }");
        sb.AppendLine("    details summary:hover { background: #f3f4f6; }");
        sb.AppendLine("    details[open] summary { margin-bottom: 0.5rem; }");
        sb.AppendLine("    .context-content { padding-left: 1rem; border-left: 2px solid #e5e7eb; }");
        sb.AppendLine("    .summary { margin-top: 2rem; }");
        sb.AppendLine("  </style>");
    }

    private static void RenderContext(TreeNode node, StringBuilder sb, IReadOnlyDictionary<string, string>? results)
    {
        var specCount = CountSpecs(node);

        sb.AppendLine("    <details open>");
        sb.AppendLine($"      <summary>{Escape(node.Description)} <small>({specCount} specs)</small></summary>");
        sb.AppendLine("      <div class=\"context-content\">");

        // Render specs in this context
        RenderSpecs(node.Specs, sb, results);

        // Render child contexts
        foreach (var child in node.Children)
        {
            RenderContext(child, sb, results);
        }

        sb.AppendLine("      </div>");
        sb.AppendLine("    </details>");
    }

    private static void RenderSpecs(List<DiscoveredSpec> specs, StringBuilder sb, IReadOnlyDictionary<string, string>? results)
    {
        if (specs.Count == 0)
            return;

        sb.AppendLine("        <ul class=\"spec-list\">");
        foreach (var spec in specs)
        {
            var status = GetSpecStatus(spec, results);
            var symbol = GetSymbol(status);
            var cssClass = GetCssClass(status, spec);
            var badge = GetBadge(spec, status);

            sb.AppendLine($"          <li class=\"{cssClass}\">{symbol} {Escape(spec.Description)}{badge}</li>");
        }
        sb.AppendLine("        </ul>");
    }

    private static string GetSpecStatus(DiscoveredSpec spec, IReadOnlyDictionary<string, string>? results)
    {
        if (results != null && results.TryGetValue(spec.Id, out var status))
            return status.ToLowerInvariant();

        if (spec.IsSkipped) return "skipped";
        if (spec.IsPending) return "pending";
        return "unknown";
    }

    private static string GetSymbol(string status) => status switch
    {
        "passed" => "✓",
        "failed" => "✗",
        "pending" => "○",
        "skipped" => "−",
        _ => "○"
    };

    private static string GetCssClass(string status, DiscoveredSpec spec)
    {
        if (spec.IsSkipped) return "skipped";
        return status switch
        {
            "passed" => "passed",
            "failed" => "failed",
            "pending" => "pending",
            _ => ""
        };
    }

    private static string GetBadge(DiscoveredSpec spec, string status)
    {
        if (spec.IsFocused)
            return "<span class=\"badge badge-focused\">FOCUSED</span>";
        if (spec.IsSkipped)
            return "<span class=\"badge badge-skipped\">SKIPPED</span>";
        if (spec.IsPending)
            return "<span class=\"badge badge-pending\">PENDING</span>";
        if (string.Equals(status, "failed", StringComparison.Ordinal))
            return "<span class=\"badge badge-failed\">FAILED</span>";
        return "";
    }

    private static void AppendSummaryStats(StringBuilder sb, IReadOnlyList<DiscoveredSpec> specs, IReadOnlyDictionary<string, string>? results)
    {
        var parts = new List<string>();

        if (results != null)
        {
            var passed = results.Values.Count(v => v.Equals("passed", StringComparison.OrdinalIgnoreCase));
            var failed = results.Values.Count(v => v.Equals("failed", StringComparison.OrdinalIgnoreCase));

            if (passed > 0) parts.Add($"<span class=\"passed\">{passed} passed</span>");
            if (failed > 0) parts.Add($"<span class=\"failed\">{failed} failed</span>");
        }

        var pending = specs.Count(s => s.IsPending);
        var skipped = specs.Count(s => s.IsSkipped);
        var focused = specs.Count(s => s.IsFocused);

        if (focused > 0) parts.Add($"<span>{focused} focused</span>");
        if (pending > 0) parts.Add($"<span class=\"pending\">{pending} pending</span>");
        if (skipped > 0) parts.Add($"<span class=\"skipped\">{skipped} skipped</span>");

        if (parts.Count > 0)
            sb.Append($": {string.Join(", ", parts)}");
    }

    private static int CountSpecs(TreeNode node)
    {
        return node.Specs.Count + node.Children.Sum(c => CountSpecs(c));
    }

    private static TreeNode BuildTree(IReadOnlyList<DiscoveredSpec> specs)
    {
        var root = new TreeNode { Description = "" };

        foreach (var spec in specs)
        {
            var current = root;

            foreach (var context in spec.ContextPath)
            {
                var existing = current.Children.Find(c => string.Equals(c.Description, context, StringComparison.Ordinal));
                if (existing == null)
                {
                    existing = new TreeNode { Description = context };
                    current.Children.Add(existing);
                }
                current = existing;
            }

            current.Specs.Add(spec);
        }

        return root;
    }

    private static string Escape(string text) => HttpUtility.HtmlEncode(text);

    private sealed class TreeNode
    {
        public string Description { get; init; } = "";
        public List<TreeNode> Children { get; } = [];
        public List<DiscoveredSpec> Specs { get; } = [];
    }
}

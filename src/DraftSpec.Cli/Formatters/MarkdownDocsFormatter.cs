using System.Text;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats specs as Markdown documentation with hierarchical headings.
/// </summary>
public sealed class MarkdownDocsFormatter : IDocsFormatter
{
    public string Format(IReadOnlyList<DiscoveredSpec> specs, DocsMetadata metadata)
    {
        var sb = new StringBuilder();

        // Title
        sb.AppendLine("# Test Specifications");
        sb.AppendLine();

        // Build tree structure
        var tree = BuildTree(specs);

        // Render tree as markdown
        foreach (var child in tree.Children)
        {
            RenderContext(child, sb, level: 2, metadata.Results);
        }

        // Render any root-level specs
        RenderSpecs(tree.Specs, sb, metadata.Results);

        // Summary
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(GetSummary(specs, metadata.Results));
        sb.AppendLine();
        sb.AppendLine();

        // Footer
        sb.Append($"*Generated {metadata.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        if (!string.IsNullOrEmpty(metadata.Source))
            sb.Append($" from `{metadata.Source}`");
        sb.AppendLine("*");

        return sb.ToString();
    }

    private static void RenderContext(TreeNode node, StringBuilder sb, int level, IReadOnlyDictionary<string, string>? results)
    {
        // Context heading (max 6 levels in markdown)
        var heading = new string('#', Math.Min(level, 6));
        sb.AppendLine($"{heading} {node.Description}");
        sb.AppendLine();

        // Render specs in this context
        RenderSpecs(node.Specs, sb, results);

        // Render child contexts
        foreach (var child in node.Children)
        {
            RenderContext(child, sb, level + 1, results);
        }
    }

    private static void RenderSpecs(List<DiscoveredSpec> specs, StringBuilder sb, IReadOnlyDictionary<string, string>? results)
    {
        if (specs.Count == 0)
            return;

        foreach (var spec in specs)
        {
            var status = GetSpecStatus(spec, results);
            var checkbox = GetCheckbox(status);
            var suffix = GetSuffix(spec, status);

            sb.AppendLine($"- {checkbox} {spec.Description}{suffix}");
        }

        sb.AppendLine();
    }

    private static string GetSpecStatus(DiscoveredSpec spec, IReadOnlyDictionary<string, string>? results)
    {
        // Check results first
        if (results != null && results.TryGetValue(spec.Id, out var status))
            return status;

        // Default based on spec flags
        if (spec.IsSkipped) return "skipped";
        if (spec.IsPending) return "pending";
        return "unknown";
    }

    private static string GetCheckbox(string status) => status switch
    {
        "passed" or "Passed" => "[x]",
        "failed" or "Failed" => "[x]", // Still checked but marked as failed in suffix
        "pending" or "Pending" => "[ ]",
        "skipped" or "Skipped" => "[ ]",
        _ => "[ ]"
    };

    private static string GetSuffix(DiscoveredSpec spec, string status)
    {
        if (spec.IsSkipped)
            return " *(skipped)*";
        if (spec.IsPending)
            return " *(pending)*";

        return status switch
        {
            "failed" or "Failed" => " **FAILED**",
            _ => ""
        };
    }

    private static string GetSummary(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyDictionary<string, string>? results)
    {
        var total = specs.Count;
        var parts = new List<string>();

        if (results != null)
        {
            var passed = results.Values.Count(v => v is "passed" or "Passed");
            var failed = results.Values.Count(v => v is "failed" or "Failed");
            var pending = specs.Count(s => s.IsPending);
            var skipped = specs.Count(s => s.IsSkipped);

            if (passed > 0) parts.Add($"{passed} passed");
            if (failed > 0) parts.Add($"{failed} failed");
            if (pending > 0) parts.Add($"{pending} pending");
            if (skipped > 0) parts.Add($"{skipped} skipped");
        }
        else
        {
            var pending = specs.Count(s => s.IsPending);
            var skipped = specs.Count(s => s.IsSkipped);
            var focused = specs.Count(s => s.IsFocused);

            if (focused > 0) parts.Add($"{focused} focused");
            if (pending > 0) parts.Add($"{pending} pending");
            if (skipped > 0) parts.Add($"{skipped} skipped");
        }

        var suffix = parts.Count > 0 ? $": {string.Join(", ", parts)}" : "";
        return $"**{total} specs**{suffix}";
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

    private sealed class TreeNode
    {
        public string Description { get; init; } = "";
        public List<TreeNode> Children { get; } = [];
        public List<DiscoveredSpec> Specs { get; } = [];
    }
}

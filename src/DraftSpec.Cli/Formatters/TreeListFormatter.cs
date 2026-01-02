using System.Text;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats specs as a hierarchical tree grouped by file and context.
/// </summary>
public sealed class TreeListFormatter : IListFormatter
{
    private readonly bool _showLineNumbers;

    public TreeListFormatter(bool showLineNumbers = true)
    {
        _showLineNumbers = showLineNumbers;
    }

    public string Format(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors)
    {
        var sb = new StringBuilder();

        // Group by file
        var byFile = specs
            .GroupBy(s => s.RelativeSourceFile)
            .OrderBy(g => g.Key);

        foreach (var fileGroup in byFile)
        {
            sb.AppendLine(fileGroup.Key);

            // Build tree structure from context paths
            var tree = BuildTree(fileGroup.ToList());
            RenderTree(tree, sb, indent: 1);

            sb.AppendLine();
        }

        // Show errors
        if (errors.Count > 0)
        {
            sb.AppendLine("Compilation Errors:");
            foreach (var error in errors)
            {
                sb.AppendLine($"  {error.RelativeSourceFile}");
                sb.AppendLine($"    {error.Message}");
            }
            sb.AppendLine();
        }

        // Summary
        sb.AppendLine(GetSummary(specs, errors));

        return sb.ToString();
    }

    private static TreeNode BuildTree(List<DiscoveredSpec> specs)
    {
        var root = new TreeNode { Description = "" };

        foreach (var spec in specs)
        {
            var current = root;

            // Navigate/create context nodes
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

            // Add spec as leaf
            current.Specs.Add(spec);
        }

        return root;
    }

    private void RenderTree(TreeNode node, StringBuilder sb, int indent)
    {
        var prefix = new string(' ', indent * 2);

        // Render child contexts first
        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            sb.AppendLine($"{prefix}{child.Description}");
            RenderTree(child, sb, indent + 1);
        }

        // Render specs
        for (var i = 0; i < node.Specs.Count; i++)
        {
            var spec = node.Specs[i];
            var isLast = i == node.Specs.Count - 1 && node.Children.Count == 0;
            var branch = isLast ? "  " : "  ";

            var icon = GetSpecIcon(spec);
            var lineInfo = _showLineNumbers ? $" (line {spec.LineNumber})" : "";
            var flags = GetSpecFlags(spec);

            sb.AppendLine($"{prefix}{branch}{icon} {spec.Description}{lineInfo}{flags}");
        }
    }

    private static string GetSpecIcon(DiscoveredSpec spec)
    {
        if (spec.HasCompilationError) return "[X]";
        if (spec.IsFocused) return "[*]";
        if (spec.IsSkipped) return "[-]";
        if (spec.IsPending) return "[?]";
        return "[.]";
    }

    private static string GetSpecFlags(DiscoveredSpec spec)
    {
        var flags = new List<string>();
        if (spec.IsFocused) flags.Add("FOCUSED");
        if (spec.IsSkipped) flags.Add("SKIPPED");
        if (spec.IsPending) flags.Add("PENDING");
        if (spec.HasCompilationError) flags.Add("ERROR");

        return flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
    }

    private static string GetSummary(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors)
    {
        var focusedCount = specs.Count(s => s.IsFocused);
        var skippedCount = specs.Count(s => s.IsSkipped);
        var pendingCount = specs.Count(s => s.IsPending);
        var errorCount = specs.Count(s => s.HasCompilationError);
        var fileCount = specs.Select(s => s.RelativeSourceFile).Distinct().Count();

        var parts = new List<string> { $"{specs.Count} specs" };
        if (focusedCount > 0) parts.Add($"{focusedCount} focused");
        if (skippedCount > 0) parts.Add($"{skippedCount} skipped");
        if (pendingCount > 0) parts.Add($"{pendingCount} pending");
        if (errorCount > 0) parts.Add($"{errorCount} with errors");

        return $"Total: {string.Join(", ", parts)} in {fileCount} files" +
               (errors.Count > 0 ? $", {errors.Count} files with compilation errors" : "");
    }

    private sealed class TreeNode
    {
        public string Description { get; init; } = "";
        public List<TreeNode> Children { get; } = [];
        public List<DiscoveredSpec> Specs { get; } = [];
    }
}

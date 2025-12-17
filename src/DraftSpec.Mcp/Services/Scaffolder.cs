using System.Text;
using DraftSpec.Mcp.Models;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Generates DraftSpec code from a structured scaffold definition.
/// </summary>
public static class Scaffolder
{
    private const string Indent = "    ";

    /// <summary>
    /// Maximum nesting depth allowed to prevent stack overflow from malicious input.
    /// </summary>
    public const int MaxDepth = 32;

    /// <summary>
    /// Generate DraftSpec code from a scaffold node structure.
    /// </summary>
    /// <param name="node">The root scaffold node.</param>
    /// <returns>Generated DraftSpec code with pending specs.</returns>
    public static string Generate(ScaffoldNode node)
    {
        var sb = new StringBuilder();
        GenerateNode(sb, node, 0);
        return sb.ToString();
    }

    private static void GenerateNode(StringBuilder sb, ScaffoldNode node, int depth)
    {
        if (depth > MaxDepth)
            throw new InvalidOperationException(
                $"Scaffold nesting depth exceeds maximum of {MaxDepth}. " +
                "This limit prevents stack overflow from deeply nested structures.");

        var indent = GetIndent(depth);
        var description = EscapeString(node.Description);

        sb.AppendLine($"{indent}describe(\"{description}\", () =>");
        sb.AppendLine($"{indent}{{");

        // Generate specs at this level
        foreach (var spec in node.Specs)
        {
            var specDescription = EscapeString(spec);
            sb.AppendLine($"{indent}{Indent}it(\"{specDescription}\");");
        }

        // Add blank line between specs and nested contexts if both exist
        if (node.Specs.Count > 0 && node.Contexts.Count > 0)
        {
            sb.AppendLine();
        }

        // Generate nested contexts
        for (var i = 0; i < node.Contexts.Count; i++)
        {
            if (i > 0)
            {
                sb.AppendLine();
            }
            GenerateNode(sb, node.Contexts[i], depth + 1);
        }

        sb.AppendLine($"{indent}}});");
    }

    private static string GetIndent(int depth)
    {
        return string.Concat(Enumerable.Repeat(Indent, depth));
    }

    private static string EscapeString(string value)
    {
        // Escape all C# string escape sequences to prevent code injection
        // Order matters: backslash must be escaped first
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\0", "\\0");
    }
}

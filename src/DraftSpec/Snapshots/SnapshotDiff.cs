using System.Text;

namespace DraftSpec.Snapshots;

/// <summary>
/// Generates human-readable diffs between expected and actual snapshot values.
/// </summary>
internal static class SnapshotDiff
{
    private const int MaxDiffLines = 50;

    /// <summary>
    /// Generate a line-by-line diff between expected and actual JSON.
    /// </summary>
    public static string Generate(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');

        var diff = new StringBuilder();
        diff.AppendLine("Expected:");
        AppendLines(diff, expectedLines, "  ");
        diff.AppendLine();
        diff.AppendLine("Received:");
        AppendLines(diff, actualLines, "  ");
        diff.AppendLine();
        diff.AppendLine("Difference:");

        var diffLines = 0;
        var maxLines = Math.Max(expectedLines.Length, actualLines.Length);

        for (var i = 0; i < maxLines && diffLines < MaxDiffLines; i++)
        {
            var exp = i < expectedLines.Length ? expectedLines[i].TrimEnd('\r') : "";
            var act = i < actualLines.Length ? actualLines[i].TrimEnd('\r') : "";

            if (!string.Equals(exp, act, StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(exp))
                {
                    diff.AppendLine($"- {exp}");
                    diffLines++;
                }

                if (!string.IsNullOrEmpty(act))
                {
                    diff.AppendLine($"+ {act}");
                    diffLines++;
                }
            }
        }

        if (diffLines >= MaxDiffLines)
        {
            diff.AppendLine($"  ... (truncated, {maxLines - MaxDiffLines} more lines differ)");
        }

        return diff.ToString();
    }

    private static void AppendLines(StringBuilder sb, string[] lines, string prefix)
    {
        var count = Math.Min(lines.Length, 10);
        for (var i = 0; i < count; i++)
        {
            sb.AppendLine($"{prefix}{lines[i].TrimEnd('\r')}");
        }

        if (lines.Length > 10)
        {
            sb.AppendLine($"{prefix}... ({lines.Length - 10} more lines)");
        }
    }
}

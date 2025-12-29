using System.Text;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats specs as flat single-line entries, easy to grep/filter.
/// Format: file:line  Context > Path > description [FLAGS]
/// </summary>
public sealed class FlatListFormatter : IListFormatter
{
    private readonly bool _showLineNumbers;

    public FlatListFormatter(bool showLineNumbers = true)
    {
        _showLineNumbers = showLineNumbers;
    }

    public string Format(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors)
    {
        var sb = new StringBuilder();

        foreach (var spec in specs.OrderBy(s => s.RelativeSourceFile).ThenBy(s => s.LineNumber))
        {
            var location = _showLineNumbers
                ? $"{spec.RelativeSourceFile}:{spec.LineNumber}"
                : spec.RelativeSourceFile;

            var flags = GetFlags(spec);

            sb.AppendLine($"{location,-50} {spec.DisplayName}{flags}");
        }

        if (errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Compilation Errors:");
            foreach (var error in errors)
            {
                sb.AppendLine($"  {error.RelativeSourceFile}: {error.Message}");
            }
        }

        // Summary
        sb.AppendLine();
        sb.AppendLine(GetSummary(specs, errors));

        return sb.ToString();
    }

    private static string GetFlags(DiscoveredSpec spec)
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

        var parts = new List<string> { $"{specs.Count} specs" };
        if (focusedCount > 0) parts.Add($"{focusedCount} focused");
        if (skippedCount > 0) parts.Add($"{skippedCount} skipped");
        if (pendingCount > 0) parts.Add($"{pendingCount} pending");

        return $"Total: {string.Join(", ", parts)}" +
               (errors.Count > 0 ? $", {errors.Count} files with errors" : "");
    }
}

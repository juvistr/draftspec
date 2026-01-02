using System.Text;
using DraftSpec.Cli.CoverageMap;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats coverage map results for human-readable console output.
/// </summary>
public sealed class ConsoleCoverageMapFormatter : ICoverageMapFormatter
{
    private const int MaxSpecsToShow = 4;

    public string Format(CoverageMapResult result, bool gapsOnly)
    {
        var sb = new StringBuilder();

        // Header with summary
        AppendHeader(sb, result);

        // Methods grouped by class
        var methodsToShow = gapsOnly ? result.UncoveredMethods : result.AllMethods;

        if (methodsToShow.Count == 0)
        {
            if (gapsOnly)
            {
                sb.AppendLine("No uncovered methods found.");
            }
            else
            {
                sb.AppendLine("No public methods found in source files.");
            }
            return sb.ToString();
        }

        var byClass = methodsToShow
            .GroupBy(m => $"{m.Method.Namespace}.{m.Method.ClassName}")
            .OrderBy(g => g.Key);

        foreach (var group in byClass)
        {
            AppendClassGroup(sb, group.Key, group.ToList(), gapsOnly);
        }

        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, CoverageMapResult result)
    {
        var summary = result.Summary;
        var covered = summary.TotalMethods - summary.Uncovered;

        sb.AppendLine($"Coverage Map: {summary.CoveragePercentage:F1}% ({covered}/{summary.TotalMethods} methods)");
        sb.AppendLine();
        sb.AppendLine($"  {GetConfidenceBadge(CoverageConfidence.High)} {summary.HighConfidence} methods");
        sb.AppendLine($"  {GetConfidenceBadge(CoverageConfidence.Medium)} {summary.MediumConfidence} methods");
        sb.AppendLine($"  {GetConfidenceBadge(CoverageConfidence.Low)} {summary.LowConfidence} methods");
        sb.AppendLine($"  {GetConfidenceBadge(CoverageConfidence.None)} {summary.Uncovered} methods");
        sb.AppendLine();
    }

    private static void AppendClassGroup(
        StringBuilder sb,
        string className,
        List<MethodCoverage> methods,
        bool gapsOnly)
    {
        sb.AppendLine($"{className}");

        foreach (var coverage in methods.OrderBy(m => m.Method.MethodName))
        {
            AppendMethodCoverage(sb, coverage, gapsOnly);
        }

        sb.AppendLine();
    }

    private static void AppendMethodCoverage(
        StringBuilder sb,
        MethodCoverage coverage,
        bool gapsOnly)
    {
        var badge = GetConfidenceBadge(coverage.Confidence);
        var method = coverage.Method;

        sb.AppendLine($"  {badge} {method.Signature} (line {method.LineNumber})");

        if (gapsOnly)
        {
            // For gaps-only, suggest what to test
            sb.AppendLine($"        Suggestion: Add spec for \"{method.MethodName}\"");
            return;
        }

        // Show covering specs (up to max)
        var specs = coverage.CoveringSpecs;
        if (specs.Count == 0)
        {
            return;
        }

        foreach (var spec in specs.Take(MaxSpecsToShow))
        {
            var confidence = spec.Confidence.ToString().ToLowerInvariant();
            sb.AppendLine($"        [{confidence}] {spec.DisplayName}");
            if (!string.IsNullOrEmpty(spec.MatchReason))
            {
                sb.AppendLine($"               {spec.MatchReason}");
            }
        }

        if (specs.Count > MaxSpecsToShow)
        {
            sb.AppendLine($"        ... and {specs.Count - MaxSpecsToShow} more");
        }
    }

    private static string GetConfidenceBadge(CoverageConfidence confidence)
    {
        return confidence switch
        {
            CoverageConfidence.High => "[HIGH]  ",
            CoverageConfidence.Medium => "[MEDIUM]",
            CoverageConfidence.Low => "[LOW]   ",
            _ => "[NONE]  "
        };
    }
}

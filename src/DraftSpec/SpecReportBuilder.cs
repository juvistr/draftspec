using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec;

/// <summary>
/// Builds a SpecReport from execution results for use with formatters.
/// </summary>
public static class SpecReportBuilder
{
    /// <summary>
    /// Build a SpecReport from a root context and its execution results.
    /// </summary>
    public static SpecReport Build(SpecContext rootContext, IReadOnlyList<SpecResult> results)
    {
        // Build O(1) lookup dictionary for results
        var resultLookup = results.ToDictionary(
            r => (r.Spec, string.Join("/", r.ContextPath)),
            r => r);

        var totalDuration = results.Sum(r => r.Duration.TotalMilliseconds);

        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary
            {
                Total = results.Count,
                Passed = results.Count(r => r.Status == SpecStatus.Passed),
                Failed = results.Count(r => r.Status == SpecStatus.Failed),
                Pending = results.Count(r => r.Status == SpecStatus.Pending),
                Skipped = results.Count(r => r.Status == SpecStatus.Skipped),
                DurationMs = totalDuration
            },
            Contexts = BuildContextTree(rootContext, resultLookup)
        };

        return report;
    }

    private static List<SpecContextReport> BuildContextTree(
        SpecContext context,
        Dictionary<(SpecDefinition Spec, string Path), SpecResult> resultLookup)
    {
        var contextList = new List<SpecContextReport>();
        BuildContextTreeRecursive(context, resultLookup, contextList, []);
        return contextList;
    }

    private static void BuildContextTreeRecursive(
        SpecContext context,
        Dictionary<(SpecDefinition Spec, string Path), SpecResult> resultLookup,
        List<SpecContextReport> targetList,
        List<string> currentPath)
    {
        var reportContext = new SpecContextReport { Description = context.Description };
        currentPath.Add(context.Description);
        var pathKey = string.Join("/", currentPath);

        // Find specs that belong to this context - O(1) lookup per spec
        foreach (var spec in context.Specs)
        {
            resultLookup.TryGetValue((spec, pathKey), out var result);

            reportContext.Specs.Add(new SpecResultReport
            {
                Description = spec.Description,
                Status = result?.Status.ToString().ToLowerInvariant() ?? "unknown",
                DurationMs = result?.Duration.TotalMilliseconds,
                Error = result?.Exception?.Message
            });
        }

        // Recursively process child contexts
        foreach (var child in context.Children)
            BuildContextTreeRecursive(child, resultLookup, reportContext.Contexts, [.. currentPath]);

        // Only add if there are specs or nested contexts
        if (reportContext.Specs.Count > 0 || reportContext.Contexts.Count > 0) targetList.Add(reportContext);
    }
}

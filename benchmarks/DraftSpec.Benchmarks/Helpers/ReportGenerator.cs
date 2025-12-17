namespace DraftSpec.Benchmarks.Helpers;

/// <summary>
/// Generates synthetic spec reports for formatter benchmarking.
/// </summary>
public static class ReportGenerator
{
    /// <summary>
    /// Creates test data with the specified number of specs and status distribution.
    /// </summary>
    /// <param name="specCount">Total number of specs</param>
    /// <param name="passRate">Percentage of specs that pass (0.0 to 1.0)</param>
    /// <param name="failRate">Percentage of specs that fail (0.0 to 1.0)</param>
    /// <param name="pendingRate">Percentage of specs that are pending (0.0 to 1.0)</param>
    /// <returns>A tuple of (SpecContext, List of SpecResults)</returns>
    public static (SpecContext Context, List<SpecResult> Results) CreateTestData(
        int specCount,
        double passRate = 0.9,
        double failRate = 0.05,
        double pendingRate = 0.05)
    {
        var context = SpecTreeGenerator.CreateBalancedTree(specCount);
        var results = GenerateResults(context, passRate, failRate, pendingRate);
        return (context, results);
    }

    /// <summary>
    /// Generates SpecResults for all specs in a context tree.
    /// </summary>
    private static List<SpecResult> GenerateResults(
        SpecContext root,
        double passRate,
        double failRate,
        double pendingRate)
    {
        var results = new List<SpecResult>();
        var random = new Random(42); // Fixed seed for reproducibility
        var allSpecs = CollectAllSpecs(root, []);

        foreach (var (spec, contextPath) in allSpecs)
        {
            var roll = random.NextDouble();
            SpecStatus status;
            Exception? exception = null;

            if (roll < passRate)
            {
                status = SpecStatus.Passed;
            }
            else if (roll < passRate + failRate)
            {
                status = SpecStatus.Failed;
                exception = new AssertionException($"Expected true but got false");
            }
            else if (roll < passRate + failRate + pendingRate)
            {
                status = SpecStatus.Pending;
            }
            else
            {
                status = SpecStatus.Skipped;
            }

            var duration = TimeSpan.FromMilliseconds(random.Next(1, 100));
            results.Add(new SpecResult(spec, status, contextPath, duration, exception));
        }

        return results;
    }

    /// <summary>
    /// Recursively collects all specs with their context paths.
    /// </summary>
    private static List<(SpecDefinition Spec, List<string> ContextPath)> CollectAllSpecs(
        SpecContext context,
        List<string> parentPath)
    {
        var results = new List<(SpecDefinition, List<string>)>();
        var currentPath = parentPath.ToList();

        if (!string.IsNullOrEmpty(context.Description)) currentPath.Add(context.Description);

        // Add specs from this context
        foreach (var spec in context.Specs) results.Add((spec, currentPath.ToList()));

        // Recurse into children
        foreach (var child in context.Children) results.AddRange(CollectAllSpecs(child, currentPath));

        return results;
    }
}
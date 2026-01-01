using DraftSpec.Coverage;

namespace DraftSpec.Middleware;

/// <summary>
/// Type-safe extension methods for accessing middleware context items.
/// Uses object keys to prevent string collisions between middleware.
/// </summary>
public static class ContextExtensions
{
    private static readonly object CoverageDataKey = new();

    /// <summary>
    /// Set coverage data for the current spec execution.
    /// </summary>
    public static void SetCoverageData(this SpecExecutionContext context, SpecCoverageData data)
        => context.Items[CoverageDataKey] = data;

    /// <summary>
    /// Get coverage data from the current spec execution, if available.
    /// </summary>
    public static SpecCoverageData? GetCoverageData(this SpecExecutionContext context)
        => context.Items.TryGetValue(CoverageDataKey, out var data)
            ? (SpecCoverageData)data
            : null;
}

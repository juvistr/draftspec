using DraftSpec.Coverage;

namespace DraftSpec.Middleware;

/// <summary>
/// Middleware that tracks code coverage per-spec execution.
/// Takes snapshots before/after each spec to calculate coverage delta.
/// </summary>
/// <remarks>
/// This middleware should be placed early in the pipeline to capture
/// coverage from all subsequent middleware and the spec itself.
///
/// Coverage data is attached to the SpecResult via the CoverageInfo property.
/// </remarks>
public class CoverageMiddleware : ISpecMiddleware
{
    private readonly ICoverageTracker _tracker;
    private readonly CoverageIndex? _index;

    /// <summary>
    /// Create coverage middleware with a tracker.
    /// </summary>
    /// <param name="tracker">Coverage tracker implementation</param>
    /// <param name="index">Optional coverage index for reverse lookups</param>
    public CoverageMiddleware(ICoverageTracker tracker, CoverageIndex? index = null)
    {
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _index = index;
    }

    /// <inheritdoc />
    public async Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> pipeline)
    {
        if (!_tracker.IsActive)
        {
            return await pipeline(context).ConfigureAwait(false);
        }

        // Take snapshot before spec execution
        var snapshot = _tracker.TakeSnapshot();

        // Execute the spec (and any downstream middleware)
        var result = await pipeline(context).ConfigureAwait(false);

        // Calculate coverage delta
        var specId = string.Join(" ", context.ContextPath.Append(context.Spec.Description));
        var coverageData = _tracker.GetCoverageSince(snapshot, specId);

        // Store in context for potential use by other middleware
        context.SetCoverageData(coverageData);

        // Update the coverage index if provided
        _index?.AddSpecCoverage(coverageData);

        // Return result with coverage info attached
        return result with { CoverageInfo = coverageData };
    }
}

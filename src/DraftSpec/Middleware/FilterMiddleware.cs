namespace DraftSpec.Middleware;

/// <summary>
/// Middleware that filters specs based on a predicate.
/// Specs that don't match the filter are skipped.
/// </summary>
public class FilterMiddleware : ISpecMiddleware
{
    private readonly Func<SpecExecutionContext, bool> _predicate;
    private readonly string _reason;

    /// <summary>
    /// Create filter middleware with a predicate.
    /// </summary>
    /// <param name="predicate">Function that returns true if the spec should run</param>
    /// <param name="reason">Optional reason shown when spec is filtered (default: "filtered")</param>
    public FilterMiddleware(Func<SpecExecutionContext, bool> predicate, string reason = "filtered")
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _reason = reason;
    }

    public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context, Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        if (!_predicate(context))
        {
            return new SpecResult(
                context.Spec,
                SpecStatus.Skipped,
                context.ContextPath);
        }

        return await next(context);
    }
}

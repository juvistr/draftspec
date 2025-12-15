namespace DraftSpec.Middleware;

/// <summary>
/// Middleware that can intercept spec execution.
/// Follows the classic middleware pattern with a next delegate.
/// </summary>
public interface ISpecMiddleware
{
    /// <summary>
    /// Execute the middleware asynchronously, optionally calling next to continue the pipeline.
    /// </summary>
    /// <param name="context">Execution context with spec info and mutable state</param>
    /// <param name="next">Delegate to call the next middleware (or core execution)</param>
    /// <returns>The spec execution result</returns>
    Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next);
}

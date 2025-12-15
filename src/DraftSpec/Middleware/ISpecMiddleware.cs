namespace DraftSpec.Middleware;

/// <summary>
/// Middleware that can intercept spec execution for cross-cutting concerns.
/// </summary>
/// <remarks>
/// Follows the classic middleware pattern with a next delegate. Middleware can:
/// <list type="bullet">
/// <item><description>Run code before the spec (e.g., setup, timing)</description></item>
/// <item><description>Run code after the spec (e.g., cleanup, logging)</description></item>
/// <item><description>Modify the result (e.g., retry on failure)</description></item>
/// <item><description>Short-circuit execution (e.g., filtering, timeout)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class LoggingMiddleware : ISpecMiddleware
/// {
///     public async Task&lt;SpecResult&gt; ExecuteAsync(SpecExecutionContext ctx, Func&lt;SpecExecutionContext, Task&lt;SpecResult&gt;&gt; next)
///     {
///         Console.WriteLine($"Starting: {ctx.Spec.Description}");
///         var result = await next(ctx);
///         Console.WriteLine($"Finished: {result.Status}");
///         return result;
///     }
/// }
/// </code>
/// </example>
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

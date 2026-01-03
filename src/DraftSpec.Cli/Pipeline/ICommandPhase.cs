namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// A phase in the command execution pipeline.
/// </summary>
/// <remarks>
/// Follows the middleware pattern, mirroring <c>ISpecMiddleware</c>. Phases can:
/// <list type="bullet">
/// <item><description>Run code before calling next (e.g., validation, setup)</description></item>
/// <item><description>Run code after calling next (e.g., cleanup, output)</description></item>
/// <item><description>Modify the context Items for downstream phases</description></item>
/// <item><description>Short-circuit by returning without calling next</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class PathResolutionPhase : ICommandPhase
/// {
///     public async Task&lt;int&gt; ExecuteAsync(
///         CommandContext context,
///         Func&lt;CommandContext, CancellationToken, Task&lt;int&gt;&gt; pipeline,
///         CancellationToken ct)
///     {
///         var resolved = Path.GetFullPath(context.Path);
///         context.Set&lt;string&gt;(ContextKeys.ProjectPath, resolved);
///         return await pipeline(context, ct);
///     }
/// }
/// </code>
/// </example>
public interface ICommandPhase
{
    /// <summary>
    /// Execute the phase asynchronously, optionally calling the next phase.
    /// </summary>
    /// <param name="context">Command context with path, console, file system, and Items dictionary.</param>
    /// <param name="pipeline">Delegate to invoke the next phase in the pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct);
}

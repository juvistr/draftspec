namespace DraftSpec.Execution;

/// <summary>
/// Strategy for executing specs within a context.
/// Implementations control how specs are executed (sequential, parallel, etc.).
/// </summary>
public interface ISpecExecutionStrategy
{
    /// <summary>
    /// Execute specs according to this strategy.
    /// </summary>
    /// <param name="context">The execution context containing specs and callbacks</param>
    /// <param name="cancellationToken">Cancellation token for aborting execution</param>
    Task ExecuteAsync(SpecExecutionStrategyContext context, CancellationToken cancellationToken);
}

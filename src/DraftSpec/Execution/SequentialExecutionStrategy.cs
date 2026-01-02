namespace DraftSpec.Execution;

/// <summary>
/// Executes specs sequentially in declaration order.
/// </summary>
public sealed class SequentialExecutionStrategy : ISpecExecutionStrategy
{
    /// <summary>
    /// Singleton instance for reuse.
    /// </summary>
    public static SequentialExecutionStrategy Instance { get; } = new();

    /// <inheritdoc/>
    public async Task ExecuteAsync(SpecExecutionStrategyContext context, CancellationToken cancellationToken)
    {
        foreach (var spec in context.Specs)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // If bail triggered, skip remaining specs
            if (context.IsBailTriggered())
            {
                var skippedResult = new SpecResult(spec, SpecStatus.Skipped, context.ContextPath);
                context.Results.Add(skippedResult);
                await context.NotifyCompleted(skippedResult).ConfigureAwait(false);
                continue;
            }

            var result = await context.RunSpec(spec, context.Context, context.ContextPath, context.HasFocused).ConfigureAwait(false);
            context.Results.Add(result);
            await context.NotifyCompleted(result).ConfigureAwait(false);

            // Check if we should bail
            if (context.BailEnabled && result.Status == SpecStatus.Failed)
            {
                context.SignalBail();
            }
        }
    }
}

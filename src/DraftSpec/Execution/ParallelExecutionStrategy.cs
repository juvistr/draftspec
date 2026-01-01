namespace DraftSpec.Execution;

/// <summary>
/// Executes specs in parallel while preserving result order.
/// </summary>
public sealed class ParallelExecutionStrategy : ISpecExecutionStrategy
{
    private readonly int _maxDegreeOfParallelism;

    /// <summary>
    /// Create a parallel execution strategy.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">
    /// Maximum number of specs to execute concurrently.
    /// Values &lt;= 0 use Environment.ProcessorCount.
    /// </param>
    public ParallelExecutionStrategy(int maxDegreeOfParallelism = 0)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism <= 0
            ? Environment.ProcessorCount
            : maxDegreeOfParallelism;
    }

    /// <summary>
    /// The maximum degree of parallelism for this strategy.
    /// </summary>
    public int MaxDegreeOfParallelism => _maxDegreeOfParallelism;

    /// <inheritdoc/>
    public async Task ExecuteAsync(SpecExecutionStrategyContext context, CancellationToken cancellationToken)
    {
        // Create indexed list to preserve order
        var indexedSpecs = context.Specs.Select((spec, index) => (spec, index)).ToList();
        var resultArray = new SpecResult[indexedSpecs.Count];
        var processedFlags = new bool[indexedSpecs.Count];

        // Link external cancellation token with bail cancellation
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,
            CancellationToken = cts.Token
        };

        try
        {
            await Parallel.ForEachAsync(indexedSpecs, options, async (item, ct) =>
            {
                var (spec, index) = item;

                // Check if bail was triggered before starting
                if (context.IsBailTriggered())
                {
                    resultArray[index] = new SpecResult(spec, SpecStatus.Skipped, context.ContextPath);
                    processedFlags[index] = true;
                    return;
                }

                var result = await context.RunSpec(spec, context.Context, context.ContextPath, context.HasFocused);
                resultArray[index] = result;
                processedFlags[index] = true;

                // Check if we should bail
                if (context.BailEnabled && result.Status == SpecStatus.Failed)
                {
                    context.SignalBail();
                    cts.Cancel();
                }
            });
        }
        catch (OperationCanceledException) when (context.IsBailTriggered() || cancellationToken.IsCancellationRequested)
        {
            // Expected when bail is triggered or external cancellation requested
        }

        // Rethrow if this was external cancellation (not bail)
        cancellationToken.ThrowIfCancellationRequested();

        // Fill in any specs that weren't processed due to cancellation
        for (var i = 0; i < resultArray.Length; i++)
        {
            if (!processedFlags[i])
            {
                resultArray[i] = new SpecResult(indexedSpecs[i].spec, SpecStatus.Skipped, context.ContextPath);
            }
        }

        // Add results in original order
        context.Results.AddRange(resultArray);

        // Notify reporters in batch (parallel notification to multiple reporters)
        await context.NotifyBatchCompleted(resultArray);
    }
}

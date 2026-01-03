namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Wrapper phase that conditionally executes an inner phase based on a predicate.
/// </summary>
/// <remarks>
/// Used internally by <see cref="CommandPipelineBuilder.UseWhen"/> to implement
/// conditional phase execution. If the predicate returns false, the inner phase
/// is skipped and the next phase in the pipeline is called directly.
/// </remarks>
internal class ConditionalPhase : ICommandPhase
{
    private readonly Func<CommandContext, bool> _predicate;
    private readonly ICommandPhase _inner;

    /// <summary>
    /// Create a conditional phase wrapper.
    /// </summary>
    /// <param name="predicate">Condition to evaluate. If true, inner phase executes.</param>
    /// <param name="inner">The phase to conditionally execute.</param>
    public ConditionalPhase(Func<CommandContext, bool> predicate, ICommandPhase inner)
    {
        _predicate = predicate;
        _inner = inner;
    }

    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        if (_predicate(context))
        {
            return _inner.ExecuteAsync(context, pipeline, ct);
        }

        return pipeline(context, ct);
    }
}

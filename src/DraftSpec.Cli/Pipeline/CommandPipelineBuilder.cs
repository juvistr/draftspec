namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Fluent builder for composing command phases into an executable pipeline delegate.
/// </summary>
/// <remarks>
/// Mirrors the middleware pattern from <c>SpecRunner</c>. Phases execute in
/// registration order: first added executes first, last added executes last.
/// </remarks>
/// <example>
/// <code>
/// var pipeline = new CommandPipelineBuilder()
///     .Use(pathResolutionPhase)
///     .Use(specDiscoveryPhase)
///     .UseWhen(ctx => ctx.Get&lt;bool&gt;(ContextKeys.Quarantine), quarantinePhase)
///     .Use(specExecutionPhase)
///     .Build();
///
/// var exitCode = await pipeline(context, cancellationToken);
/// </code>
/// </example>
public class CommandPipelineBuilder
{
    private readonly List<ICommandPhase> _phases = [];

    /// <summary>
    /// Add a phase to the pipeline.
    /// </summary>
    /// <param name="phase">The phase to add.</param>
    /// <returns>This builder for method chaining.</returns>
    public CommandPipelineBuilder Use(ICommandPhase phase)
    {
        ArgumentNullException.ThrowIfNull(phase);
        _phases.Add(phase);
        return this;
    }

    /// <summary>
    /// Add a phase that only executes when the predicate returns true.
    /// </summary>
    /// <param name="predicate">Condition to evaluate against the context.</param>
    /// <param name="phase">The phase to conditionally execute.</param>
    /// <returns>This builder for method chaining.</returns>
    public CommandPipelineBuilder UseWhen(Func<CommandContext, bool> predicate, ICommandPhase phase)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(phase);
        _phases.Add(new ConditionalPhase(predicate, phase));
        return this;
    }

    /// <summary>
    /// Build the pipeline into an executable delegate.
    /// </summary>
    /// <returns>
    /// A delegate that accepts a <see cref="CommandContext"/> and <see cref="CancellationToken"/>,
    /// executes all phases in order, and returns an exit code.
    /// </returns>
    public Func<CommandContext, CancellationToken, Task<int>> Build()
    {
        // Terminal handler returns success (exit code 0)
        Func<CommandContext, CancellationToken, Task<int>> pipeline =
            (_, _) => Task.FromResult(0);

        // Wrap phases in reverse order so first-added executes first
        foreach (var phase in ((IEnumerable<ICommandPhase>)_phases).Reverse())
        {
            var current = pipeline;
            pipeline = (ctx, ct) => phase.ExecuteAsync(ctx, current, ct);
        }

        return pipeline;
    }
}

using DraftSpec.Cli.Options;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Executes all selected spec files.
/// Uses the InProcessSpecRunner with filter options from context.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[SpecFiles]</c></para>
/// <para><b>Optional:</b> <c>Items[Filter]</c>, <c>Items[Parallel]</c></para>
/// <para><b>Produces:</b> <c>Items[RunResults]</c> (InProcessRunSummary)</para>
/// <para><b>Short-circuits:</b> Returns 0 if no spec files</para>
/// </remarks>
public class SpecExecutionPhase : ICommandPhase
{
    private readonly IInProcessSpecRunnerFactory _runnerFactory;

    public SpecExecutionPhase(IInProcessSpecRunnerFactory runnerFactory)
    {
        _runnerFactory = runnerFactory;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var specFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
        if (specFiles == null || specFiles.Count == 0)
        {
            context.Console.WriteLine("No spec files to run.");
            return 0;
        }

        var filter = context.Get<FilterOptions>(ContextKeys.Filter) ?? new FilterOptions();
        var parallel = context.Get<bool>(ContextKeys.Parallel);

        // Create runner with all filter options
        var runner = _runnerFactory.Create(
            filter.FilterTags,
            filter.ExcludeTags,
            filter.FilterName,
            filter.ExcludeName,
            filter.FilterContext,
            filter.ExcludeContext);

        // Execute specs
        var summary = await runner.RunAllAsync(specFiles, parallel, ct);

        context.Set(ContextKeys.RunResults, summary);
        return await pipeline(context, ct);
    }
}

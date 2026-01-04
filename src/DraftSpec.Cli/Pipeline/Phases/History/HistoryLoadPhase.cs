using DraftSpec.Cli.History;

namespace DraftSpec.Cli.Pipeline.Phases.History;

/// <summary>
/// Loads spec execution history from the project's .draftspec directory.
/// </summary>
/// <remarks>
/// Requires: <see cref="ContextKeys.ProjectPath"/>
/// Produces: <see cref="ContextKeys.History"/> (SpecHistory)
/// </remarks>
public class HistoryLoadPhase : ICommandPhase
{
    private readonly ISpecHistoryService _historyService;

    public HistoryLoadPhase(ISpecHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var projectPath = context.Get<string>(ContextKeys.ProjectPath)!;
        var history = await _historyService.LoadAsync(projectPath, ct);
        context.Set(ContextKeys.History, history);
        return await pipeline(context, ct);
    }
}

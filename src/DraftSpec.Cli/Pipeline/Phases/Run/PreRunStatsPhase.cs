using DraftSpec.Cli.Services;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Collects and displays pre-run statistics before spec execution.
/// Handles StatsOnly mode which exits early after showing stats.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[SpecFiles]</c>, <c>Items[ProjectPath]</c></para>
/// <para><b>Optional:</b> <c>Items[StatsOnly]</c>, <c>Items[NoStats]</c></para>
/// <para><b>Short-circuits:</b> Returns 0 or 2 if StatsOnly mode</para>
/// </remarks>
public class PreRunStatsPhase : ICommandPhase
{
    private readonly ISpecStatsCollector _statsCollector;

    public PreRunStatsPhase(ISpecStatsCollector statsCollector)
    {
        _statsCollector = statsCollector;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var noStats = context.Get<bool>(ContextKeys.NoStats);
        var statsOnly = context.Get<bool>(ContextKeys.StatsOnly);

        // If NoStats is set and we're not in StatsOnly mode, skip stats entirely
        if (noStats && !statsOnly)
            return await pipeline(context, ct);

        var specFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
        if (specFiles == null || specFiles.Count == 0)
            return await pipeline(context, ct);

        var projectPath = context.Get<string>(ContextKeys.ProjectPath) ?? ".";
        var stats = await _statsCollector.CollectAsync(specFiles, projectPath, ct);

        var presenter = new ConsolePresenter(context.Console, watchMode: false);
        presenter.ShowPreRunStats(stats);

        // If StatsOnly mode, exit early with appropriate exit code
        // Exit code 2 signals focus mode is active (useful for CI)
        if (statsOnly)
            return stats.HasFocusMode ? 2 : 0;

        return await pipeline(context, ct);
    }
}

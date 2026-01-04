using System.Text.RegularExpressions;
using DraftSpec.Cli.History;
using DraftSpec.Cli.Options;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Filters out flaky specs when quarantine mode is enabled.
/// Adds flaky spec patterns to the exclude filter.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[Quarantine]</c>, <c>Items[ProjectPath]</c></para>
/// <para><b>Optional:</b> <c>Items[History]</c> (loads if not present), <c>Items[Filter]</c></para>
/// <para><b>Modifies:</b> <c>Items[Filter]</c> (adds to ExcludeName), <c>Items[History]</c> (sets if loaded)</para>
/// </remarks>
public class QuarantinePhase : ICommandPhase
{
    private readonly ISpecHistoryService _historyService;

    public QuarantinePhase(ISpecHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var quarantine = context.Get<bool>(ContextKeys.Quarantine);
        if (!quarantine)
            return await pipeline(context, ct);

        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return 1;
        }

        // Load history (or reuse if already loaded by HistoryLoadPhase)
        var history = context.Get<SpecHistory>(ContextKeys.History);
        if (history == null)
        {
            history = await _historyService.LoadAsync(projectPath, ct);
            context.Set(ContextKeys.History, history);
        }

        var flakySpecs = _historyService.GetFlakySpecs(history);
        if (flakySpecs.Count == 0)
            return await pipeline(context, ct);

        // Build exclude pattern from flaky display names
        context.Console.ForegroundColor = ConsoleColor.DarkGray;
        context.Console.WriteLine($"Quarantining {flakySpecs.Count} flaky spec(s)");
        context.Console.ResetColor();

        var flakyPattern = string.Join("|", flakySpecs.Select(f => Regex.Escape(f.DisplayName)));
        var excludePattern = $"^({flakyPattern})$";

        // Merge with existing filter
        var filter = context.Get<FilterOptions>(ContextKeys.Filter) ?? new FilterOptions();
        filter.ExcludeName = string.IsNullOrEmpty(filter.ExcludeName)
            ? excludePattern
            : $"({filter.ExcludeName})|{excludePattern}";
        context.Set(ContextKeys.Filter, filter);

        return await pipeline(context, ct);
    }
}

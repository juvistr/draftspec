using DraftSpec.Cli.History;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Records execution results to the history file.
/// Used for flaky test detection and estimation.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[RunResults]</c>, <c>Items[ProjectPath]</c></para>
/// <para><b>Optional:</b> <c>Items[NoHistory]</c> to skip recording</para>
/// <para><b>Side-effect:</b> Writes to .draftspec/history.json</para>
/// </remarks>
public class HistoryRecordPhase : ICommandPhase
{
    private readonly ISpecHistoryService _historyService;

    public HistoryRecordPhase(ISpecHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var noHistory = context.Get<bool>(ContextKeys.NoHistory);
        if (noHistory)
            return await pipeline(context, ct);

        var results = context.Get<InProcessRunSummary>(ContextKeys.RunResults);
        if (results == null)
            return await pipeline(context, ct);

        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
            return await pipeline(context, ct);

        // Extract run records from results
        var records = ExtractRunRecords(results, projectPath);
        if (records.Count > 0)
        {
            await _historyService.RecordRunAsync(projectPath, records, ct);
        }

        return await pipeline(context, ct);
    }

    private static List<SpecRunRecord> ExtractRunRecords(
        InProcessRunSummary summary,
        string projectPath)
    {
        var records = new List<SpecRunRecord>();
        foreach (var result in summary.Results)
        {
            var relativePath = Path.GetRelativePath(projectPath, result.SpecFile);
            foreach (var ctx in result.Report.Contexts)
            {
                ExtractSpecsFromContext(ctx, [], relativePath, records);
            }
        }
        return records;
    }

    private static void ExtractSpecsFromContext(
        SpecContextReport context,
        List<string> contextPath,
        string relativePath,
        List<SpecRunRecord> records)
    {
        var currentPath = new List<string>(contextPath) { context.Description };

        foreach (var spec in context.Specs)
        {
            var specId = GenerateSpecId(relativePath, currentPath, spec.Description);
            var displayName = GenerateDisplayName(currentPath, spec.Description);

            records.Add(new SpecRunRecord
            {
                SpecId = specId,
                DisplayName = displayName,
                Status = spec.Status.ToLowerInvariant(),
                DurationMs = spec.DurationMs ?? 0,
                ErrorMessage = spec.Error
            });
        }

        foreach (var child in context.Contexts)
            ExtractSpecsFromContext(child, currentPath, relativePath, records);
    }

    /// <summary>
    /// Generates a stable spec ID from file path and context path.
    /// Format: "relative/path.spec.csx:Context1/Context2/SpecDescription"
    /// </summary>
    private static string GenerateSpecId(
        string relativePath,
        List<string> contextPath,
        string description)
    {
        var pathPart = string.Join("/", contextPath);
        return $"{relativePath}:{pathPart}/{description}";
    }

    /// <summary>
    /// Generates a human-readable display name.
    /// Format: "Context1 > Context2 > SpecDescription"
    /// </summary>
    private static string GenerateDisplayName(
        List<string> contextPath,
        string description)
    {
        return string.Join(" > ", contextPath) + " > " + description;
    }
}

using DraftSpec.Cli.DependencyGraph;
using DraftSpec.Cli.Services;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Filters spec files to only those affected by changed files.
/// Uses git to detect changes and dependency graph to find affected specs.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c>, <c>Items[SpecFiles]</c></para>
/// <para><b>Optional:</b> <c>Items[AffectedBy]</c>, <c>Items[DryRun]</c></para>
/// <para><b>Modifies:</b> <c>Items[SpecFiles]</c> (filters to affected specs only)</para>
/// <para><b>Short-circuits:</b> Returns 0 if no changed files or no affected specs; Returns 1 on git error</para>
/// </remarks>
public class ImpactAnalysisPhase : ICommandPhase
{
    private readonly IGitService _gitService;
    private readonly IPathComparer _pathComparer;
    private readonly IDependencyGraphBuilder _graphBuilder;

    public ImpactAnalysisPhase(
        IGitService gitService,
        IPathComparer pathComparer,
        IDependencyGraphBuilder graphBuilder)
    {
        _gitService = gitService;
        _pathComparer = pathComparer;
        _graphBuilder = graphBuilder;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var affectedBy = context.Get<string>(ContextKeys.AffectedBy);
        if (string.IsNullOrEmpty(affectedBy))
            return await pipeline(context, ct);

        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return 1;
        }

        var specFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
        if (specFiles == null || specFiles.Count == 0)
        {
            context.Console.WriteLine("No spec files to analyze.");
            return 0;
        }

        var dryRun = context.Get<bool>(ContextKeys.DryRun);
        var console = context.Console;

        console.ForegroundColor = ConsoleColor.DarkGray;
        console.WriteLine($"Analyzing impact of changes: {affectedBy}");
        console.ResetColor();

        // Get changed files from git
        IReadOnlyList<string> changedFiles;
        try
        {
            changedFiles = await _gitService.GetChangedFilesAsync(affectedBy, projectPath, ct);
        }
        catch (InvalidOperationException ex)
        {
            console.WriteError($"Failed to get changed files: {ex.Message}");
            return 1;
        }

        if (changedFiles.Count == 0)
        {
            console.WriteLine("No changed files detected.");
            return 0;
        }

        // Build dependency graph
        var graph = await _graphBuilder.BuildAsync(projectPath, cancellationToken: ct);

        // Get affected specs
        var affectedSpecs = graph.GetAffectedSpecs(changedFiles);

        // Filter to only specs that exist in our discovered spec files
        var specFileSet = new HashSet<string>(specFiles, _pathComparer.Comparer);
        var filteredSpecs = affectedSpecs
            .Where(s => specFileSet.Contains(s))
            .ToList();

        // Show impact analysis summary
        console.ForegroundColor = ConsoleColor.DarkGray;
        console.WriteLine($"Changed files: {changedFiles.Count}");
        console.WriteLine($"Affected specs: {filteredSpecs.Count} of {specFiles.Count}");
        console.ResetColor();
        console.WriteLine();

        // If dry run, output affected specs and exit
        if (dryRun)
        {
            console.WriteLine("Affected spec files (dry run):");
            foreach (var spec in filteredSpecs.OrderBy(s => s))
            {
                console.WriteLine($"  {Path.GetRelativePath(projectPath, spec)}");
            }
            return 0;
        }

        if (filteredSpecs.Count == 0)
        {
            console.WriteLine("No affected specs to run.");
            return 0;
        }

        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, filteredSpecs);
        return await pipeline(context, ct);
    }
}

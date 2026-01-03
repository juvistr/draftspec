using DraftSpec.Cli.CoverageMap;
using DraftSpec.Cli.Services;

namespace DraftSpec.Cli.Pipeline.Phases.CoverageMap;

/// <summary>
/// Computes coverage mapping between source methods and specs.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c>, <c>Items[SourcePath]</c>, <c>Items[SourceFiles]</c></para>
/// <para><b>Optional:</b> <c>Items[SpecPath]</c>, <c>Items[NamespaceFilter]</c></para>
/// <para><b>Produces:</b> <c>Items[CoverageMapResult]</c></para>
/// </remarks>
public sealed class CoverageMapPhase : ICommandPhase
{
    private readonly ICoverageMapService _coverageMapService;
    private readonly ISpecFinder _specFinder;

    public CoverageMapPhase(ICoverageMapService coverageMapService, ISpecFinder specFinder)
    {
        _coverageMapService = coverageMapService;
        _specFinder = specFinder;
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return 1;
        }

        var sourceFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SourceFiles);
        if (sourceFiles is null || sourceFiles.Count == 0)
        {
            context.Console.WriteError("SourceFiles not set. Run SourceDiscoveryPhase first.");
            return 1;
        }

        // If no methods after filtering, report it
        if (sourceFiles.Count == 0)
        {
            context.Console.WriteLine("No public methods found in source files.");
            return 0;
        }

        // Determine spec path
        var sourcePath = context.Get<string>(ContextKeys.SourcePath);
        var specPath = context.Get<string>(ContextKeys.SpecPath);
        if (string.IsNullOrEmpty(specPath))
        {
            specPath = projectPath;
        }
        else
        {
            specPath = Path.GetFullPath(specPath);
        }

        // Find spec files - use specPath as base to allow specs outside project root
        IReadOnlyList<string> specFiles;
        try
        {
            specFiles = _specFinder.FindSpecs(specPath, specPath);
        }
        catch (ArgumentException ex)
        {
            context.Console.WriteError(ex.Message);
            return 1;
        }

        if (specFiles.Count == 0)
        {
            context.Console.WriteError("No spec files found.");
            return 1;
        }

        // Compute coverage
        var namespaceFilter = context.Get<string>(ContextKeys.NamespaceFilter);
        CoverageMapResult result;

        try
        {
            result = await _coverageMapService.ComputeCoverageAsync(
                sourceFiles,
                specFiles,
                projectPath,
                sourcePath != null ? Path.GetRelativePath(projectPath, sourcePath) : null,
                Path.GetRelativePath(projectPath, specPath),
                namespaceFilter,
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            context.Console.WriteError($"Coverage analysis failed: {ex.Message}");
            return 1;
        }

        if (result.AllMethods.Count == 0)
        {
            var message = string.IsNullOrEmpty(namespaceFilter)
                ? "No public methods found in source files."
                : $"No methods found matching namespace filter: {namespaceFilter}";
            context.Console.WriteLine(message);
            return 0;
        }

        context.Set(ContextKeys.CoverageMapResult, result);
        return await pipeline(context, ct);
    }
}

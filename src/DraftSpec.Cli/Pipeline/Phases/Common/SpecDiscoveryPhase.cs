namespace DraftSpec.Cli.Pipeline.Phases.Common;

/// <summary>
/// Discovers spec files at the resolved path, setting <see cref="ContextKeys.SpecFiles"/>.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c> is set</para>
/// <para><b>Produces:</b> <c>Items[SpecFiles]</c> - list of spec file paths</para>
/// <para><b>Short-circuits:</b> If no spec files found (returns 0)</para>
/// </remarks>
public class SpecDiscoveryPhase : ICommandPhase
{
    private readonly ISpecFinder _specFinder;

    /// <summary>
    /// Create a new spec discovery phase.
    /// </summary>
    /// <param name="specFinder">Finder for locating spec files.</param>
    public SpecDiscoveryPhase(ISpecFinder specFinder)
    {
        _specFinder = specFinder;
    }

    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return Task.FromResult(1);
        }

        IReadOnlyList<string> specFiles;
        try
        {
            specFiles = _specFinder.FindSpecs(context.Path, projectPath);
        }
        catch (ArgumentException ex)
        {
            // FindSpecs throws ArgumentException for invalid paths or no specs
            context.Console.WriteError(ex.Message);
            return Task.FromResult(1);
        }

        if (specFiles.Count == 0)
        {
            context.Console.WriteLine("No spec files found.");
            return Task.FromResult(0);
        }

        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, specFiles);

        return pipeline(context, ct);
    }
}

namespace DraftSpec.Cli.Pipeline.Phases.Common;

/// <summary>
/// Discovers and resolves project information from .csproj files.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c> - directory to search for .csproj</para>
/// <para><b>Produces:</b> <c>Items[ProjectInfo]</c> - project info (nullable if not found)</para>
/// <para><b>Warns:</b> If no .csproj found or project info cannot be resolved</para>
/// </remarks>
public class ProjectDiscoveryPhase : ICommandPhase
{
    private readonly IProjectResolver _projectResolver;

    public ProjectDiscoveryPhase(IProjectResolver projectResolver)
    {
        _projectResolver = projectResolver;
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
            context.Console.WriteError("ProjectPath not set");
            return Task.FromResult(1);
        }

        // Find .csproj file
        var csprojPath = _projectResolver.FindProject(projectPath);
        ProjectInfo? projectInfo = null;

        if (csprojPath == null)
        {
            context.Console.WriteWarning("No .csproj found. Continuing without project reference.");
        }
        else
        {
            projectInfo = _projectResolver.GetProjectInfo(csprojPath);
            if (projectInfo == null)
            {
                context.Console.WriteWarning($"Could not get project info for {Path.GetFileName(csprojPath)}");
            }
        }

        // Set in context (may be null)
        context.Set(ContextKeys.ProjectInfo, projectInfo);

        return pipeline(context, ct);
    }
}

namespace DraftSpec.Cli;

/// <summary>
/// Abstraction for resolving project information.
/// </summary>
public interface IProjectResolver
{
    /// <summary>
    /// Find the first .csproj file in the given directory.
    /// </summary>
    string? FindProject(string directory);

    /// <summary>
    /// Query MSBuild for project output path and target framework.
    /// </summary>
    ProjectInfo? GetProjectInfo(string csprojPath);
}

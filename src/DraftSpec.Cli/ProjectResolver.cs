namespace DraftSpec.Cli;

/// <summary>
/// Resolves project information by querying MSBuild.
/// </summary>
public class ProjectResolver
{
    public record ProjectInfo(string TargetPath, string TargetFramework);

    /// <summary>
    /// Find the first .csproj file in the given directory.
    /// </summary>
    public string? FindProject(string directory)
    {
        var projects = Directory.GetFiles(directory, "*.csproj");
        return projects.Length > 0 ? projects[0] : null;
    }

    /// <summary>
    /// Query MSBuild for project output path and target framework.
    /// </summary>
    public ProjectInfo? GetProjectInfo(string csprojPath)
    {
        var targetPath = GetMSBuildProperty(csprojPath, "TargetPath");
        var targetFramework = GetMSBuildProperty(csprojPath, "TargetFramework");

        if (targetPath == null)
            return null;

        return new ProjectInfo(targetPath, targetFramework ?? "net10.0");
    }

    private string? GetMSBuildProperty(string csprojPath, string property)
    {
        var result = ProcessHelper.RunDotnet(
            ["msbuild", csprojPath, $"-getProperty:{property}"],
            Path.GetDirectoryName(csprojPath));

        var output = result.Output.Trim();
        return result.Success && !string.IsNullOrEmpty(output) ? output : null;
    }
}
using System.Diagnostics;

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
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"msbuild \"{csprojPath}\" -getProperty:{property}",
            WorkingDirectory = Path.GetDirectoryName(csprojPath),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            return null;

        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        return process.ExitCode == 0 && !string.IsNullOrEmpty(output) ? output : null;
    }
}

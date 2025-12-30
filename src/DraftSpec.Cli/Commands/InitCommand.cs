using System.Text;
using DraftSpec.Cli.Options;

namespace DraftSpec.Cli.Commands;

public class InitCommand : ICommand<InitOptions>
{
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;
    private readonly IProjectResolver _projectResolver;

    public InitCommand(IConsole console, IFileSystem fileSystem, IProjectResolver projectResolver)
    {
        _console = console;
        _fileSystem = fileSystem;
        _projectResolver = projectResolver;
    }

    public Task<int> ExecuteAsync(InitOptions options, CancellationToken ct = default)
    {
        var directory = Path.GetFullPath(options.Path);

        if (!_fileSystem.DirectoryExists(directory))
            throw new ArgumentException($"Directory not found: {directory}");

        // Find project
        var csproj = _projectResolver.FindProject(directory);
        ProjectInfo? info = null;

        if (csproj == null)
        {
            _console.WriteWarning("No .csproj found. Creating spec_helper without project reference.");
        }
        else
        {
            info = _projectResolver.GetProjectInfo(csproj);
            if (info == null)
            {
                _console.WriteWarning($"Could not get project info for {Path.GetFileName(csproj)}");
            }
        }

        // Generate spec_helper.csx
        var specHelperPath = Path.Combine(directory, "spec_helper.csx");
        if (_fileSystem.FileExists(specHelperPath) && !options.Force)
        {
            _console.WriteLine("spec_helper.csx already exists (use --force to overwrite)");
        }
        else
        {
            var specHelper = GenerateSpecHelper(info, directory);
            _fileSystem.WriteAllText(specHelperPath, specHelper);
            _console.WriteSuccess("Created spec_helper.csx");
        }

        // Generate omnisharp.json
        var omnisharpPath = Path.Combine(directory, "omnisharp.json");
        if (_fileSystem.FileExists(omnisharpPath) && !options.Force)
        {
            _console.WriteLine("omnisharp.json already exists (use --force to overwrite)");
        }
        else
        {
            var omnisharp = GenerateOmnisharp(info?.TargetFramework ?? "net10.0");
            _fileSystem.WriteAllText(omnisharpPath, omnisharp);
            _console.WriteSuccess("Created omnisharp.json");
        }

        return Task.FromResult(0);
    }

    private static string GenerateSpecHelper(ProjectInfo? info, string directory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#r \"nuget: DraftSpec, *\""); // Wildcard includes prereleases

        if (info != null)
        {
            // Make the path relative to the directory
            var relativePath = Path.GetRelativePath(directory, info.TargetPath);
            sb.AppendLine($"#r \"{relativePath}\"");
        }

        sb.AppendLine();
        sb.AppendLine("using static DraftSpec.Dsl;");
        sb.AppendLine();
        sb.AppendLine("// Add shared fixtures below:");

        return sb.ToString();
    }

    private static string GenerateOmnisharp(string targetFramework)
    {
        return $$"""
                 {
                   "script": {
                     "enableScriptNuGetReferences": true,
                     "defaultTargetFramework": "{{targetFramework}}"
                   }
                 }
                 """;
    }
}
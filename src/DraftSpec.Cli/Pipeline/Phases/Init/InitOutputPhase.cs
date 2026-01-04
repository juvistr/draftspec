using System.Text;

namespace DraftSpec.Cli.Pipeline.Phases.Init;

/// <summary>
/// Generates spec_helper.csx and omnisharp.json files.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c> - directory where files will be created</para>
/// <para><b>Reads:</b> <c>Items[ProjectInfo]</c> - optional project info for assembly reference</para>
/// <para><b>Reads:</b> <c>Items[Force]</c> - whether to overwrite existing files</para>
/// </remarks>
public class InitOutputPhase : ICommandPhase
{
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

        var force = context.Get<bool>(ContextKeys.Force);
        var projectInfo = context.Get<ProjectInfo>(ContextKeys.ProjectInfo);

        // Generate spec_helper.csx
        var specHelperPath = Path.Combine(projectPath, "spec_helper.csx");
        if (context.FileSystem.FileExists(specHelperPath) && !force)
        {
            context.Console.WriteLine("spec_helper.csx already exists (use --force to overwrite)");
        }
        else
        {
            var specHelper = GenerateSpecHelper(projectInfo, projectPath);
            context.FileSystem.WriteAllText(specHelperPath, specHelper);
            context.Console.WriteSuccess("Created spec_helper.csx");
        }

        // Generate omnisharp.json
        var omnisharpPath = Path.Combine(projectPath, "omnisharp.json");
        if (context.FileSystem.FileExists(omnisharpPath) && !force)
        {
            context.Console.WriteLine("omnisharp.json already exists (use --force to overwrite)");
        }
        else
        {
            var omnisharp = GenerateOmnisharp(projectInfo?.TargetFramework ?? "net10.0");
            context.FileSystem.WriteAllText(omnisharpPath, omnisharp);
            context.Console.WriteSuccess("Created omnisharp.json");
        }

        return pipeline(context, ct);
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

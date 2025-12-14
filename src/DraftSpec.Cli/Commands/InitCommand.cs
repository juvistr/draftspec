using System.Text;

namespace DraftSpec.Cli.Commands;

public static class InitCommand
{
    public static int Execute(CliOptions options)
    {
        var resolver = new ProjectResolver();
        var directory = Path.GetFullPath(options.Path);

        if (!Directory.Exists(directory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Directory not found: {directory}");
            Console.ResetColor();
            return 1;
        }

        // Find project
        var csproj = resolver.FindProject(directory);
        ProjectResolver.ProjectInfo? info = null;

        if (csproj == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No .csproj found. Creating spec_helper without project reference.");
            Console.ResetColor();
        }
        else
        {
            info = resolver.GetProjectInfo(csproj);
            if (info == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Could not get project info for {Path.GetFileName(csproj)}");
                Console.ResetColor();
            }
        }

        // Generate spec_helper.csx
        var specHelperPath = Path.Combine(directory, "spec_helper.csx");
        if (File.Exists(specHelperPath) && !options.Force)
        {
            Console.WriteLine("spec_helper.csx already exists (use --force to overwrite)");
        }
        else
        {
            var specHelper = GenerateSpecHelper(info, directory);
            File.WriteAllText(specHelperPath, specHelper);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Created spec_helper.csx");
            Console.ResetColor();
        }

        // Generate omnisharp.json
        var omnisharpPath = Path.Combine(directory, "omnisharp.json");
        if (File.Exists(omnisharpPath) && !options.Force)
        {
            Console.WriteLine("omnisharp.json already exists (use --force to overwrite)");
        }
        else
        {
            var omnisharp = GenerateOmnisharp(info?.TargetFramework ?? "net10.0");
            File.WriteAllText(omnisharpPath, omnisharp);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Created omnisharp.json");
            Console.ResetColor();
        }

        return 0;
    }

    private static string GenerateSpecHelper(ProjectResolver.ProjectInfo? info, string directory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#r \"nuget: DraftSpec\"");

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

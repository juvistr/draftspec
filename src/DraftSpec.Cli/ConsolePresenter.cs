namespace DraftSpec.Cli;

public class ConsolePresenter
{
    private readonly bool _watchMode;

    public ConsolePresenter(bool watchMode = false)
    {
        _watchMode = watchMode;
    }

    public void Clear()
    {
        if (_watchMode)
        {
            Console.Clear();
        }
    }

    public void ShowHeader(IReadOnlyList<string> specFiles, bool parallel = false)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        var mode = parallel && specFiles.Count > 1 ? " in parallel" : "";
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Running {specFiles.Count} spec file(s){mode}...");
        Console.ResetColor();
    }

    public void ShowBuilding(string project)
    {
        var name = Path.GetFileNameWithoutExtension(project);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  Building {name}... ");
        Console.ResetColor();
    }

    public void ShowBuildResult(BuildResult result)
    {
        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ok");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("failed");
            if (!string.IsNullOrWhiteSpace(result.Error))
                Console.WriteLine(result.Error);
        }
        Console.ResetColor();
    }

    public void ShowSpecsStarting()
    {
        Console.WriteLine();
    }

    public void ShowResult(SpecRunResult result, string basePath)
    {
        var relativePath = Path.GetRelativePath(basePath, result.SpecFile);

        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            Console.Write(result.Output);
            if (!result.Output.EndsWith('\n'))
                Console.WriteLine();
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(result.Error);
            Console.ResetColor();
            if (!result.Error.EndsWith('\n'))
                Console.WriteLine();
        }
    }

    public void ShowSummary(RunSummary summary)
    {
        Console.WriteLine();

        if (summary.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("PASS");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("FAIL");
        }
        Console.ResetColor();

        Console.WriteLine($"  {summary.TotalSpecs} file(s) in {summary.TotalDuration.TotalSeconds:F2}s");
    }

    public void ShowWatching()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Watching for changes... (Ctrl+C to quit)");
        Console.ResetColor();
    }

    public void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
    }

    public void ShowRerunning()
    {
        Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("File changed, re-running...");
        Console.ResetColor();
        Console.WriteLine();
    }
}

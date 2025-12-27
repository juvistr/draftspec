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
        if (_watchMode) Console.Clear();
    }

    public void ShowHeader(IReadOnlyList<string> specFiles, bool parallel = false, bool isPartialRun = false)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        var mode = parallel && specFiles.Count > 1 ? " in parallel" : "";

        if (isPartialRun && specFiles.Count == 1)
        {
            var fileName = Path.GetFileName(specFiles[0]);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Running {fileName}...");
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Running {specFiles.Count} spec file(s){mode}...");
        }

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

    public void ShowBuildSkipped(string project)
    {
        var name = Path.GetFileNameWithoutExtension(project);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  Building {name}... ");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("skipped (no changes)");
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

    public void ShowCoverageReport(string reportPath)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Coverage report: {reportPath}");
        Console.ResetColor();
    }

    public void ShowCoverageReportGenerated(string format, string path)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Coverage {format} report: {path}");
        Console.ResetColor();
    }

    public void ShowCoverageSummary(double linePercent, double branchPercent)
    {
        var lineColor = GetCoverageColor(linePercent);
        var branchColor = GetCoverageColor(branchPercent);

        Console.Write("Coverage: ");
        Console.ForegroundColor = lineColor;
        Console.Write($"{linePercent:F1}% lines");
        Console.ResetColor();
        Console.Write(", ");
        Console.ForegroundColor = branchColor;
        Console.Write($"{branchPercent:F1}% branches");
        Console.ResetColor();
        Console.WriteLine();
    }

    public void ShowCoverageThresholdWarnings(IEnumerable<string> failures)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Coverage threshold warning:");
        foreach (var failure in failures)
        {
            Console.WriteLine($"  {failure}");
        }
        Console.ResetColor();
    }

    private static ConsoleColor GetCoverageColor(double percent) => percent switch
    {
        >= 80 => ConsoleColor.Green,
        >= 50 => ConsoleColor.Yellow,
        _ => ConsoleColor.Red
    };
}
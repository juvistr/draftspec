namespace DraftSpec.Cli;

public class ConsolePresenter
{
    private readonly IConsole _console;
    private readonly bool _watchMode;

    public ConsolePresenter(IConsole? console = null, bool watchMode = false)
    {
        _console = console ?? new SystemConsole();
        _watchMode = watchMode;
    }

    public void Clear()
    {
        if (_watchMode) _console.Clear();
    }

    public void ShowHeader(IReadOnlyList<string> specFiles, bool parallel = false, bool isPartialRun = false)
    {
        _console.ForegroundColor = ConsoleColor.DarkGray;
        var mode = parallel && specFiles.Count > 1 ? " in parallel" : "";

        if (isPartialRun && specFiles.Count == 1)
        {
            var fileName = Path.GetFileName(specFiles[0]);
            _console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Running {fileName}...");
        }
        else
        {
            _console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Running {specFiles.Count} spec file(s){mode}...");
        }

        _console.ResetColor();
    }

    public void ShowBuilding(string project)
    {
        var name = Path.GetFileNameWithoutExtension(project);
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.Write($"  Building {name}... ");
        _console.ResetColor();
    }

    public void ShowBuildResult(BuildResult result)
    {
        if (result.Success)
        {
            _console.ForegroundColor = ConsoleColor.Green;
            _console.WriteLine("ok");
        }
        else
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine("failed");
            if (!string.IsNullOrWhiteSpace(result.Error))
                _console.WriteLine(result.Error);
        }

        _console.ResetColor();
    }

    public void ShowBuildSkipped(string project)
    {
        var name = Path.GetFileNameWithoutExtension(project);
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.Write($"  Building {name}... ");
        _console.ForegroundColor = ConsoleColor.DarkYellow;
        _console.WriteLine("skipped (no changes)");
        _console.ResetColor();
    }

    public void ShowSpecsStarting()
    {
        _console.WriteLine();
    }

    public void ShowResult(SpecRunResult result, string basePath)
    {
        var relativePath = Path.GetRelativePath(basePath, result.SpecFile);

        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            _console.Write(result.Output);
            if (!result.Output.EndsWith('\n'))
                _console.WriteLine();
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.Write(result.Error);
            _console.ResetColor();
            if (!result.Error.EndsWith('\n'))
                _console.WriteLine();
        }
    }

    public void ShowSummary(RunSummary summary)
    {
        _console.WriteLine();

        if (summary.Success)
        {
            _console.ForegroundColor = ConsoleColor.Green;
            _console.Write("PASS");
        }
        else
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.Write("FAIL");
        }

        _console.ResetColor();

        _console.WriteLine($"  {summary.TotalSpecs} file(s) in {summary.TotalDuration.TotalSeconds:F2}s");
    }

    public void ShowWatching()
    {
        _console.WriteLine();
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.WriteLine("Watching for changes... (Ctrl+C to quit)");
        _console.ResetColor();
    }

    public void ShowError(string message)
    {
        _console.ForegroundColor = ConsoleColor.Red;
        _console.WriteLine($"Error: {message}");
        _console.ResetColor();
    }

    public void ShowRerunning()
    {
        Clear();
        _console.ForegroundColor = ConsoleColor.Cyan;
        _console.WriteLine("File changed, re-running...");
        _console.ResetColor();
        _console.WriteLine();
    }

    public void ShowCoverageReport(string reportPath)
    {
        _console.WriteLine();
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.WriteLine($"Coverage report: {reportPath}");
        _console.ResetColor();
    }

    public void ShowCoverageReportGenerated(string format, string path)
    {
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.WriteLine($"Coverage {format} report: {path}");
        _console.ResetColor();
    }

    public void ShowCoverageSummary(double linePercent, double branchPercent)
    {
        var lineColor = GetCoverageColor(linePercent);
        var branchColor = GetCoverageColor(branchPercent);

        _console.Write("Coverage: ");
        _console.ForegroundColor = lineColor;
        _console.Write($"{linePercent:F1}% lines");
        _console.ResetColor();
        _console.Write(", ");
        _console.ForegroundColor = branchColor;
        _console.Write($"{branchPercent:F1}% branches");
        _console.ResetColor();
        _console.WriteLine();
    }

    public void ShowCoverageThresholdWarnings(IEnumerable<string> failures)
    {
        _console.WriteLine();
        _console.ForegroundColor = ConsoleColor.Yellow;
        _console.WriteLine("Coverage threshold warning:");
        foreach (var failure in failures)
        {
            _console.WriteLine($"  {failure}");
        }
        _console.ResetColor();
    }

    private static ConsoleColor GetCoverageColor(double percent) => percent switch
    {
        >= 80 => ConsoleColor.Green,
        >= 50 => ConsoleColor.Yellow,
        _ => ConsoleColor.Red
    };
}

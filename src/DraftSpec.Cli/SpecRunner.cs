using System.Diagnostics;

namespace DraftSpec.Cli;

public record SpecRunResult(
    string SpecFile,
    string Output,
    string Error,
    int ExitCode,
    TimeSpan Duration)
{
    public bool Success => ExitCode == 0;
}

public record RunSummary(
    IReadOnlyList<SpecRunResult> Results,
    TimeSpan TotalDuration)
{
    public bool Success => Results.All(r => r.Success);
    public int TotalSpecs => Results.Count;
    public int Passed => Results.Count(r => r.Success);
    public int Failed => Results.Count(r => !r.Success);
}

public class SpecRunner
{
    public SpecRunResult Run(string specFile)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;
        var fileName = Path.GetFileName(fullPath);

        var stopwatch = Stopwatch.StartNew();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"script \"{fileName}\" --no-cache",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        stopwatch.Stop();

        return new SpecRunResult(
            specFile,
            output,
            error,
            process.ExitCode,
            stopwatch.Elapsed);
    }

    public RunSummary RunAll(IReadOnlyList<string> specFiles)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<SpecRunResult>();

        foreach (var specFile in specFiles)
        {
            results.Add(Run(specFile));
        }

        stopwatch.Stop();
        return new RunSummary(results, stopwatch.Elapsed);
    }
}

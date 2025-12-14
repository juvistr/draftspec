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

public record BuildResult(bool Success, string Output, string Error);

public class SpecRunner
{
    public event Action<string>? OnBuildStarted;
    public event Action<BuildResult>? OnBuildCompleted;

    public SpecRunResult Run(string specFile)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;
        var fileName = Path.GetFileName(fullPath);

        // Build any projects in the spec's directory first
        BuildProjects(workingDir);

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

    public RunSummary RunAll(IReadOnlyList<string> specFiles, bool parallel = false)
    {
        var stopwatch = Stopwatch.StartNew();

        // Collect unique directories and build each once (always sequential)
        var directories = specFiles
            .Select(f => Path.GetDirectoryName(Path.GetFullPath(f))!)
            .Distinct()
            .ToList();

        foreach (var dir in directories)
        {
            BuildProjects(dir);
        }

        List<SpecRunResult> results;
        if (parallel && specFiles.Count > 1)
        {
            // Run specs in parallel
            var resultsBag = new System.Collections.Concurrent.ConcurrentDictionary<int, SpecRunResult>();
            Parallel.ForEach(
                specFiles.Select((file, index) => (file, index)),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                item => resultsBag[item.index] = RunSpec(item.file));

            // Restore original order
            results = resultsBag.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
        }
        else
        {
            // Run specs sequentially
            results = new List<SpecRunResult>();
            foreach (var specFile in specFiles)
            {
                results.Add(RunSpec(specFile));
            }
        }

        stopwatch.Stop();
        return new RunSummary(results, stopwatch.Elapsed);
    }

    private void BuildProjects(string directory)
    {
        var projects = Directory.GetFiles(directory, "*.csproj");
        foreach (var project in projects)
        {
            OnBuildStarted?.Invoke(project);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{project}\" --nologo -v q",
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            OnBuildCompleted?.Invoke(new BuildResult(process.ExitCode == 0, output, error));
        }
    }

    private SpecRunResult RunSpec(string specFile)
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

    /// <summary>
    /// Run a spec file with JSON output mode.
    /// Modifies the script to call run(json: true) instead of run().
    /// </summary>
    public SpecRunResult RunWithJson(string specFile)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;
        var fileName = Path.GetFileName(fullPath);

        // Read the script and modify run() to run(json: true)
        var scriptContent = File.ReadAllText(fullPath);
        var modifiedScript = scriptContent
            .Replace("run();", "run(json: true);")
            .Replace("run()", "run(json: true)");

        // Write to temp file
        var tempFile = Path.Combine(workingDir, $".{Path.GetFileNameWithoutExtension(fileName)}.json.csx");
        File.WriteAllText(tempFile, modifiedScript);

        try
        {
            var stopwatch = Stopwatch.StartNew();

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"script \"{Path.GetFileName(tempFile)}\" --no-cache",
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
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}

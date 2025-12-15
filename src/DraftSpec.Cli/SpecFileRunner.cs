using System.Diagnostics;
using System.Text.RegularExpressions;

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

public class SpecFileRunner
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
        var result = ProcessHelper.RunDotnet(["script", fileName, "--no-cache"], workingDir);
        stopwatch.Stop();

        return new SpecRunResult(
            specFile,
            result.Output,
            result.Error,
            result.ExitCode,
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

            var result = ProcessHelper.RunDotnet(["build", project, "--nologo", "-v", "q"], directory);

            OnBuildCompleted?.Invoke(new BuildResult(result.Success, result.Output, result.Error));
        }
    }

    private SpecRunResult RunSpec(string specFile)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;
        var fileName = Path.GetFileName(fullPath);

        var stopwatch = Stopwatch.StartNew();
        var result = ProcessHelper.RunDotnet(["script", fileName, "--no-cache"], workingDir);
        stopwatch.Stop();

        return new SpecRunResult(
            specFile,
            result.Output,
            result.Error,
            result.ExitCode,
            stopwatch.Elapsed);
    }

    /// <summary>
    /// Run a spec file with JSON output mode.
    /// Modifies the script to call run(json: true) instead of run().
    /// </summary>
    /// <remarks>
    /// Security: Uses atomic file creation (FileMode.CreateNew) to prevent
    /// symlink race condition attacks (CWE-367 TOCTOU). If an attacker
    /// pre-creates a symlink at the temp path, the operation fails safely
    /// instead of following the symlink.
    /// </remarks>
    public SpecRunResult RunWithJson(string specFile)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;

        // Read the script and modify run() to run(json: true) using regex for safety
        var scriptContent = File.ReadAllText(fullPath);
        var modifiedScript = Regex.Replace(
            scriptContent,
            @"\brun\s*\(\s*\)\s*;?",
            "run(json: true);",
            RegexOptions.None);

        // Security: Create temp file with atomic operation
        // FileMode.CreateNew fails if file already exists (prevents symlink attack)
        // FileShare.None ensures exclusive access during write
        var tempFileName = $".draftspec-{Guid.NewGuid():N}.csx";
        var tempFile = Path.Combine(workingDir, tempFileName);

        try
        {
            // Atomic file creation - fails if any file (including symlink) exists at path
            using (var fs = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fs))
            {
                writer.Write(modifiedScript);
            }

            var stopwatch = Stopwatch.StartNew();
            var result = ProcessHelper.RunDotnet(["script", tempFileName, "--no-cache"], workingDir);
            stopwatch.Stop();

            return new SpecRunResult(
                specFile,
                result.Output,
                result.Error,
                result.ExitCode,
                stopwatch.Elapsed);
        }
        finally
        {
            // Clean up temp file with robust error handling
            try
            {
                if (File.Exists(tempFile))
                {
                    File.SetAttributes(tempFile, FileAttributes.Normal);
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // Best-effort cleanup - don't fail the operation if cleanup fails
            }
        }
    }
}

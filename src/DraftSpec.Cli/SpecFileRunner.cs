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

public record BuildResult(bool Success, string Output, string Error, bool Skipped = false);

public partial class SpecFileRunner : ISpecFileRunner
{
    private readonly Dictionary<string, DateTime> _lastBuildTime = new();
    private readonly Dictionary<string, DateTime> _lastSourceModified = new();
    private readonly bool _useFileBased;

    /// <summary>
    /// Regex to match #r "nuget: Package, Version" directives.
    /// </summary>
    [GeneratedRegex(@"#r\s+""nuget:\s*([^,""]+),?\s*([^""]*)""")]
    private static partial Regex NuGetRefPattern();

    public SpecFileRunner()
    {
        _useFileBased = ProcessHelper.SupportsFileBasedApps;
    }

    public event Action<string>? OnBuildStarted;
    public event Action<BuildResult>? OnBuildCompleted;
    public event Action<string>? OnBuildSkipped;

    public SpecRunResult Run(string specFile)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;

        // Build any projects in the spec's directory first
        BuildProjects(workingDir);

        // Use file-based if: .NET 10+ AND no #load directives
        if (_useFileBased && !UsesLoadDirective(fullPath)) return RunAsFileBased(fullPath, workingDir);

        return RunAsDotnetScript(fullPath, workingDir);
    }

    public RunSummary RunAll(IReadOnlyList<string> specFiles, bool parallel = false)
    {
        var stopwatch = Stopwatch.StartNew();

        // Collect unique directories and build each once
        var directories = specFiles
            .Select(f => Path.GetDirectoryName(Path.GetFullPath(f))!)
            .Distinct()
            .ToList();

        if (parallel && directories.Count > 1)
            // Build directories in parallel (projects within each directory still sequential)
            Parallel.ForEach(
                directories,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                dir => BuildProjects(dir));
        else
            // Build directories sequentially
            foreach (var dir in directories)
                BuildProjects(dir);

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
            foreach (var specFile in specFiles) results.Add(RunSpec(specFile));
        }

        stopwatch.Stop();
        return new RunSummary(results, stopwatch.Elapsed);
    }

    private void BuildProjects(string directory)
    {
        var (projects, projectDir) = FindProjectFiles(directory);
        if (projects.Length == 0) return;

        // Check if rebuild is needed (incremental build support)
        // Use projectDir for caching since that's where source files are
        if (!NeedsRebuild(projectDir))
        {
            foreach (var project in projects) OnBuildSkipped?.Invoke(project);
            return;
        }

        foreach (var project in projects)
        {
            OnBuildStarted?.Invoke(project);

            var result = ProcessHelper.RunDotnet(["build", project, "--nologo", "-v", "q"], projectDir);

            OnBuildCompleted?.Invoke(new BuildResult(result.Success, result.Output, result.Error));
        }

        // Update build cache on successful build - use projectDir as key
        _lastBuildTime[projectDir] = DateTime.UtcNow;
        _lastSourceModified[projectDir] = GetLatestSourceModification(projectDir);
    }

    /// <summary>
    /// Find .csproj files for a spec directory.
    /// Searches the directory and parent directories up to 3 levels.
    /// Returns the projects and the directory they were found in.
    /// </summary>
    private static (string[] Projects, string ProjectDirectory) FindProjectFiles(string specDirectory)
    {
        var currentDir = specDirectory;
        const int maxLevels = 3;

        for (var i = 0; i < maxLevels; i++)
        {
            var projects = Directory.GetFiles(currentDir, "*.csproj");
            if (projects.Length > 0) return (projects, currentDir);

            var parentDir = Directory.GetParent(currentDir)?.FullName;
            if (parentDir == null || parentDir == currentDir) break;

            currentDir = parentDir;
        }

        return ([], specDirectory);
    }

    /// <summary>
    /// Check if a directory needs to be rebuilt by comparing source file timestamps.
    /// </summary>
    private bool NeedsRebuild(string directory)
    {
        // Never built before - need to build
        if (!_lastBuildTime.TryGetValue(directory, out var lastBuild))
            return true;

        // Get current latest modification time
        var currentLatest = GetLatestSourceModification(directory);

        // If any source file was modified after last build, rebuild
        if (!_lastSourceModified.TryGetValue(directory, out var lastModified))
            return true;

        return currentLatest > lastModified;
    }

    /// <summary>
    /// Get the latest modification time of any source file (.cs, .csproj) in a directory.
    /// </summary>
    private static DateTime GetLatestSourceModification(string directory)
    {
        var latest = DateTime.MinValue;

        // Check .cs files
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var modified = File.GetLastWriteTimeUtc(file);
            if (modified > latest) latest = modified;
        }

        // Check .csproj files
        foreach (var file in Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly))
        {
            var modified = File.GetLastWriteTimeUtc(file);
            if (modified > latest) latest = modified;
        }

        return latest;
    }

    /// <summary>
    /// Clear the build cache, forcing a full rebuild on next run.
    /// </summary>
    public void ClearBuildCache()
    {
        _lastBuildTime.Clear();
        _lastSourceModified.Clear();
    }

    private SpecRunResult RunSpec(string specFile)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;

        // Use file-based if: .NET 10+ AND no #load directives
        if (_useFileBased && !UsesLoadDirective(fullPath)) return RunAsFileBased(fullPath, workingDir);

        return RunAsDotnetScript(fullPath, workingDir);
    }

    /// <summary>
    /// Check if a spec file uses #load directive (requires dotnet-script).
    /// </summary>
    private static bool UsesLoadDirective(string specFile)
    {
        var content = File.ReadAllText(specFile);
        return content.Contains("#load ");
    }

    /// <summary>
    /// Run spec using .NET 10 file-based app (faster, no dotnet-script dependency).
    /// </summary>
    private SpecRunResult RunAsFileBased(string specFile, string workingDir)
    {
        string? tempFile = null;
        try
        {
            tempFile = TransformToFileBased(specFile);
            var stopwatch = Stopwatch.StartNew();
            var result = ProcessHelper.RunDotnet(["run", tempFile], workingDir);
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
            if (tempFile != null)
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                }
        }
    }

    /// <summary>
    /// Run spec using dotnet-script (fallback for #load directives or older SDKs).
    /// </summary>
    private static SpecRunResult RunAsDotnetScript(string specFile, string workingDir)
    {
        var fileName = Path.GetFileName(specFile);
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
    /// Transform a CSX spec file to .NET 10 file-based app format.
    /// Converts #r "nuget:..." to #:package directives.
    /// </summary>
    private static string TransformToFileBased(string specFile)
    {
        var content = File.ReadAllText(specFile);

        // Transform #r "nuget: Package, Version" → #:package Package@Version
        content = NuGetRefPattern().Replace(content, match =>
        {
            var package = match.Groups[1].Value.Trim();
            var version = match.Groups[2].Value.Trim();
            return string.IsNullOrEmpty(version)
                ? $"#:package {package}@*"
                : $"#:package {package}@{version}";
        });

        // Add reflection property for JSON serialization (required for AOT compatibility)
        content = "#:property JsonSerializerIsReflectionEnabledByDefault=true\n" + content;

        // Write to temp .cs file
        var tempDir = Path.Combine(Path.GetTempPath(), "draftspec-cli");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, $"{Guid.NewGuid():N}.cs");
        File.WriteAllText(tempFile, content);

        // Create NuGet.config for local package resolution (development only)
        EnsureNuGetConfig(tempDir);

        return tempFile;
    }

    private static bool _nugetConfigCreated;
    private static readonly object _nugetConfigLock = new();

    /// <summary>
    /// Create NuGet.config in temp directory for local package resolution.
    /// Uses /tmp/draftspec-packages as additional source for development.
    /// </summary>
    private static void EnsureNuGetConfig(string tempDir)
    {
        if (_nugetConfigCreated) return;

        lock (_nugetConfigLock)
        {
            if (_nugetConfigCreated) return;

            var nugetConfigPath = Path.Combine(tempDir, "NuGet.config");
            var localPackagesPath = Path.Combine(Path.GetTempPath(), "draftspec-packages");

            // Only create if local packages directory exists (development mode)
            if (Directory.Exists(localPackagesPath) && !File.Exists(nugetConfigPath))
            {
                var nugetConfig = $"""
                                   <?xml version="1.0" encoding="utf-8"?>
                                   <configuration>
                                     <packageSources>
                                       <add key="local" value="{localPackagesPath}" />
                                       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                     </packageSources>
                                   </configuration>
                                   """;
                File.WriteAllText(nugetConfigPath, nugetConfig);
            }

            _nugetConfigCreated = true;
        }
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

    /// <summary>
    /// Run a spec file with JSON output via FileReporter.
    /// Uses environment variable to trigger automatic FileReporter registration.
    /// </summary>
    /// <remarks>
    /// This approach separates JSON output (written to temp file) from console output
    /// (stays on stdout), avoiding issues where Console.WriteLine in specs corrupts JSON.
    /// </remarks>
    public SpecRunResult RunWithJsonReporter(string specFile)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;

        // Build any projects in the spec's directory first
        BuildProjects(workingDir);

        // Create temp file path for JSON output
        var jsonOutputFile = Path.Combine(Path.GetTempPath(), $".draftspec-{Guid.NewGuid():N}.json");
        var envVars = new Dictionary<string, string>
        {
            ["DRAFTSPEC_JSON_OUTPUT_FILE"] = jsonOutputFile
        };

        // Use file-based if: .NET 10+ AND no #load directives
        if (_useFileBased && !UsesLoadDirective(fullPath))
            return RunAsFileBasedWithJson(fullPath, workingDir, jsonOutputFile, envVars);

        return RunAsDotnetScriptWithJson(fullPath, workingDir, jsonOutputFile, envVars);
    }

    private SpecRunResult RunAsFileBasedWithJson(
        string specFile,
        string workingDir,
        string jsonOutputFile,
        Dictionary<string, string> envVars)
    {
        string? tempFile = null;
        try
        {
            tempFile = TransformToFileBased(specFile);
            var stopwatch = Stopwatch.StartNew();
            var result = ProcessHelper.RunDotnet(["run", tempFile], workingDir, envVars);
            stopwatch.Stop();

            var jsonOutput = File.Exists(jsonOutputFile)
                ? File.ReadAllText(jsonOutputFile)
                : "{}";

            return new SpecRunResult(
                specFile,
                jsonOutput,
                result.Error,
                result.ExitCode,
                stopwatch.Elapsed);
        }
        finally
        {
            if (tempFile != null)
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                }

            try
            {
                if (File.Exists(jsonOutputFile)) File.Delete(jsonOutputFile);
            }
            catch
            {
            }
        }
    }

    private static SpecRunResult RunAsDotnetScriptWithJson(
        string specFile,
        string workingDir,
        string jsonOutputFile,
        Dictionary<string, string> envVars)
    {
        var fileName = Path.GetFileName(specFile);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = ProcessHelper.RunDotnet(
                ["script", fileName, "--no-cache"],
                workingDir,
                envVars);

            stopwatch.Stop();

            // Read JSON from temp file (not stdout)
            var jsonOutput = File.Exists(jsonOutputFile)
                ? File.ReadAllText(jsonOutputFile)
                : "{}";

            return new SpecRunResult(
                specFile,
                jsonOutput, // JSON from file, not process stdout
                result.Error,
                result.ExitCode,
                stopwatch.Elapsed);
        }
        finally
        {
            // Clean up temp file
            try
            {
                if (File.Exists(jsonOutputFile)) File.Delete(jsonOutputFile);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
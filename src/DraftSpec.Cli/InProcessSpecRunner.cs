using System.Diagnostics;
using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Scripting;

namespace DraftSpec.Cli;

/// <summary>
/// Result of running specs from a single file.
/// </summary>
public record InProcessRunResult(
    string SpecFile,
    SpecReport Report,
    TimeSpan Duration,
    Exception? Error = null)
{
    public bool Success => Error == null && Report.Summary.Failed == 0;
}

/// <summary>
/// Summary of running multiple spec files.
/// </summary>
public record InProcessRunSummary(
    IReadOnlyList<InProcessRunResult> Results,
    TimeSpan TotalDuration)
{
    public bool Success => Results.All(r => r.Success);
    public int TotalSpecs => Results.Sum(r => r.Report.Summary.Total);
    public int Passed => Results.Sum(r => r.Report.Summary.Passed);
    public int Failed => Results.Sum(r => r.Report.Summary.Failed);
    public int Pending => Results.Sum(r => r.Report.Summary.Pending);
    public int Skipped => Results.Sum(r => r.Report.Summary.Skipped);
}

/// <summary>
/// Runs spec files in-process using CsxScriptHost.
/// Replaces the subprocess-based SpecFileRunner with direct Roslyn execution.
/// </summary>
public class InProcessSpecRunner : IInProcessSpecRunner
{
    private readonly Dictionary<string, DateTime> _lastBuildTime = new();
    private readonly Dictionary<string, DateTime> _lastSourceModified = new();
    private readonly string? _filterTags;
    private readonly string? _excludeTags;
    private readonly string? _filterName;
    private readonly string? _excludeName;

    public InProcessSpecRunner(
        string? filterTags = null,
        string? excludeTags = null,
        string? filterName = null,
        string? excludeName = null)
    {
        _filterTags = filterTags;
        _excludeTags = excludeTags;
        _filterName = filterName;
        _excludeName = excludeName;
    }

    public event Action<string>? OnBuildStarted;
    public event Action<BuildResult>? OnBuildCompleted;
    public event Action<string>? OnBuildSkipped;

    /// <summary>
    /// Run a single spec file and return the report.
    /// </summary>
    public async Task<InProcessRunResult> RunFileAsync(string specFile, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(specFile);
        var workingDir = Path.GetDirectoryName(fullPath)!;

        // Build any projects in the spec's directory first
        BuildProjects(workingDir);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Find output directory for assembly resolution
            var outputDir = FindOutputDirectory(workingDir);

            // Reset DSL state before execution
            Dsl.Reset();

            // Execute script via CsxScriptHost
            var scriptHost = new CsxScriptHost(outputDir);
            var rootContext = await scriptHost.ExecuteAsync(fullPath, ct);

            if (rootContext == null)
            {
                // No specs defined - return empty report
                stopwatch.Stop();
                return new InProcessRunResult(
                    specFile,
                    new SpecReport
                    {
                        Timestamp = DateTime.UtcNow,
                        Source = fullPath,
                        Summary = new SpecSummary()
                    },
                    stopwatch.Elapsed);
            }

            // Build runner with filters
            var builder = BuildRunner();
            var runner = builder.Build();

            // Execute specs
            var results = runner.Run(rootContext);

            // Build report
            var report = SpecReportBuilder.Build(rootContext, results);
            report.Source = fullPath;

            stopwatch.Stop();
            return new InProcessRunResult(specFile, report, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new InProcessRunResult(
                specFile,
                new SpecReport
                {
                    Timestamp = DateTime.UtcNow,
                    Source = fullPath,
                    Summary = new SpecSummary { Failed = 1 }
                },
                stopwatch.Elapsed,
                ex);
        }
        finally
        {
            Dsl.Reset();
        }
    }

    /// <summary>
    /// Run all spec files, optionally in parallel.
    /// </summary>
    public async Task<InProcessRunSummary> RunAllAsync(
        IReadOnlyList<string> specFiles,
        bool parallel = false,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Collect unique directories and build each once
        var directories = specFiles
            .Select(f => Path.GetDirectoryName(Path.GetFullPath(f))!)
            .Distinct()
            .ToList();

        // Build all directories first (sequential - builds should be fast with incremental support)
        foreach (var dir in directories)
        {
            BuildProjects(dir);
        }

        // Run specs
        List<InProcessRunResult> results;
        if (parallel && specFiles.Count > 1)
        {
            var tasks = specFiles.Select(f => RunFileAsync(f, ct));
            results = (await Task.WhenAll(tasks)).ToList();
        }
        else
        {
            results = [];
            foreach (var specFile in specFiles)
            {
                ct.ThrowIfCancellationRequested();
                results.Add(await RunFileAsync(specFile, ct));
            }
        }

        stopwatch.Stop();
        return new InProcessRunSummary(results, stopwatch.Elapsed);
    }

    /// <summary>
    /// Build a SpecRunnerBuilder with configured filters.
    /// </summary>
    private SpecRunnerBuilder BuildRunner()
    {
        var builder = new SpecRunnerBuilder();

        // Add tag filter
        if (!string.IsNullOrEmpty(_filterTags))
        {
            var tags = _filterTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tags.Length > 0)
            {
                builder.WithTagFilter(tags);
            }
        }

        // Add tag exclusion
        if (!string.IsNullOrEmpty(_excludeTags))
        {
            var tags = _excludeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tags.Length > 0)
            {
                builder.WithoutTags(tags);
            }
        }

        // Add name filter
        if (!string.IsNullOrEmpty(_filterName))
        {
            builder.WithNameFilter(_filterName);
        }

        // Add name exclusion
        if (!string.IsNullOrEmpty(_excludeName))
        {
            builder.WithNameExcludeFilter(_excludeName);
        }

        return builder;
    }

    /// <summary>
    /// Find the output directory for a spec's project.
    /// </summary>
    private static string FindOutputDirectory(string specDirectory)
    {
        // Look for bin/Debug/net* folders
        var (_, projectDir) = FindProjectFiles(specDirectory);
        var binDir = Path.Combine(projectDir, "bin", "Debug");

        if (Directory.Exists(binDir))
        {
            // Find the first net* folder (e.g., net10.0, net9.0)
            var netDir = Directory.EnumerateDirectories(binDir, "net*").FirstOrDefault();
            if (netDir != null)
            {
                return netDir;
            }
        }

        // Fall back to the spec directory
        return specDirectory;
    }

    private void BuildProjects(string directory)
    {
        var (projects, projectDir) = FindProjectFiles(directory);
        if (projects.Length == 0) return;

        // Check if rebuild is needed (incremental build support)
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

        // Update build cache on successful build
        _lastBuildTime[projectDir] = DateTime.UtcNow;
        _lastSourceModified[projectDir] = GetLatestSourceModification(projectDir);
    }

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

    private bool NeedsRebuild(string directory)
    {
        if (!_lastBuildTime.TryGetValue(directory, out var lastBuild))
            return true;

        var currentLatest = GetLatestSourceModification(directory);

        if (!_lastSourceModified.TryGetValue(directory, out var lastModified))
            return true;

        return currentLatest > lastModified;
    }

    private static DateTime GetLatestSourceModification(string directory)
    {
        var latest = DateTime.MinValue;

        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var modified = File.GetLastWriteTimeUtc(file);
            if (modified > latest) latest = modified;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly))
        {
            var modified = File.GetLastWriteTimeUtc(file);
            if (modified > latest) latest = modified;
        }

        return latest;
    }

    public void ClearBuildCache()
    {
        _lastBuildTime.Clear();
        _lastSourceModified.Clear();
    }
}

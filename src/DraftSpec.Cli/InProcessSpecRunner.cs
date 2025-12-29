using DraftSpec.Configuration;
using DraftSpec.Formatters;

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
    private readonly ITimeProvider _timeProvider;
    private readonly IProjectBuilder _projectBuilder;
    private readonly ISpecScriptExecutor _scriptExecutor;
    private readonly string? _filterTags;
    private readonly string? _excludeTags;
    private readonly string? _filterName;
    private readonly string? _excludeName;

    public InProcessSpecRunner(
        string? filterTags = null,
        string? excludeTags = null,
        string? filterName = null,
        string? excludeName = null,
        ITimeProvider? timeProvider = null,
        IProjectBuilder? projectBuilder = null,
        ISpecScriptExecutor? scriptExecutor = null)
    {
        _filterTags = filterTags;
        _excludeTags = excludeTags;
        _filterName = filterName;
        _excludeName = excludeName;

        // Use defaults for backward compatibility
        _timeProvider = timeProvider ?? new SystemTimeProvider();
        _projectBuilder = projectBuilder ?? CreateDefaultProjectBuilder();
        _scriptExecutor = scriptExecutor ?? new RoslynSpecScriptExecutor();

        // Wire up build events from project builder to this runner
        _projectBuilder.OnBuildStarted += project => OnBuildStarted?.Invoke(project);
        _projectBuilder.OnBuildCompleted += result => OnBuildCompleted?.Invoke(result);
        _projectBuilder.OnBuildSkipped += project => OnBuildSkipped?.Invoke(project);
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
        _projectBuilder.BuildProjects(workingDir);

        var stopwatch = _timeProvider.StartNew();
        try
        {
            // Find output directory for assembly resolution
            var outputDir = _projectBuilder.FindOutputDirectory(workingDir);

            // Reset DSL state before execution
            Dsl.Reset();

            // Execute script via script executor
            var rootContext = await _scriptExecutor.ExecuteAsync(fullPath, outputDir, ct);

            if (rootContext == null)
            {
                // No specs defined - return empty report
                stopwatch.Stop();
                return new InProcessRunResult(
                    specFile,
                    new SpecReport
                    {
                        Timestamp = _timeProvider.UtcNow,
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
                    Timestamp = _timeProvider.UtcNow,
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
        var stopwatch = _timeProvider.StartNew();

        // Collect unique directories and build each once
        var directories = specFiles
            .Select(f => Path.GetDirectoryName(Path.GetFullPath(f))!)
            .Distinct()
            .ToList();

        // Build all directories first (sequential - builds should be fast with incremental support)
        foreach (var dir in directories)
        {
            _projectBuilder.BuildProjects(dir);
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

    public void ClearBuildCache()
    {
        _projectBuilder.ClearBuildCache();
    }

    /// <summary>
    /// Create a default project builder with production implementations.
    /// </summary>
    private static IProjectBuilder CreateDefaultProjectBuilder()
    {
        var fileSystem = new FileSystem();
        var processRunner = new SystemProcessRunner();
        var buildCache = new InMemoryBuildCache();
        var timeProvider = new SystemTimeProvider();
        return new DotnetProjectBuilder(fileSystem, processRunner, buildCache, timeProvider);
    }
}

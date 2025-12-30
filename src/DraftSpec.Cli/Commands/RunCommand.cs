using System.Security;
using System.Text.RegularExpressions;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;
using DraftSpec.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Commands;

public class RunCommand : ICommand<RunOptions>
{
    private readonly ISpecFinder _specFinder;
    private readonly IInProcessSpecRunnerFactory _runnerFactory;
    private readonly IConsole _console;
    private readonly ICliFormatterRegistry _formatterRegistry;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironment _environment;
    private readonly ISpecStatsCollector _statsCollector;
    private readonly ISpecPartitioner _partitioner;

    public RunCommand(
        ISpecFinder specFinder,
        IInProcessSpecRunnerFactory runnerFactory,
        IConsole console,
        ICliFormatterRegistry formatterRegistry,
        IFileSystem fileSystem,
        IEnvironment environment,
        ISpecStatsCollector statsCollector,
        ISpecPartitioner partitioner)
    {
        _specFinder = specFinder;
        _runnerFactory = runnerFactory;
        _console = console;
        _formatterRegistry = formatterRegistry;
        _fileSystem = fileSystem;
        _environment = environment;
        _statsCollector = statsCollector;
        _partitioner = partitioner;
    }

    public async Task<int> ExecuteAsync(RunOptions options, CancellationToken ct = default)
    {
        // Apply line number filtering if specified
        var filterName = options.Filter.FilterName;
        if (options.Filter.LineFilters is { Count: > 0 })
        {
            var lineFilterPattern = await BuildLineFilterPatternAsync(options, ct);
            if (!string.IsNullOrEmpty(lineFilterPattern))
            {
                filterName = string.IsNullOrEmpty(filterName)
                    ? lineFilterPattern
                    : $"({filterName})|({lineFilterPattern})";
            }
            else
            {
                _console.WriteError("No specs found at the specified line numbers.");
                return 1;
            }
        }

        var runner = _runnerFactory.Create(
            options.Filter.FilterTags,
            options.Filter.ExcludeTags,
            filterName,
            options.Filter.ExcludeName,
            options.Filter.FilterContext,
            options.Filter.ExcludeContext);

        var specFiles = _specFinder.FindSpecs(options.Path);
        if (specFiles.Count == 0)
        {
            _console.WriteLine("No spec files found.");
            return 0;
        }

        // Set up presenter for console output
        var presenter = new ConsolePresenter(_console, watchMode: false);

        // Get project path for stats collection and partitioning
        var projectPath = Path.GetFullPath(options.Path);
        if (_fileSystem.FileExists(projectPath))
            projectPath = Path.GetDirectoryName(projectPath)!;

        // Apply partitioning if specified
        if (options.Partition.IsEnabled)
        {
            var partitionResult = await _partitioner.PartitionAsync(
                specFiles,
                options.Partition.Total!.Value,
                options.Partition.Index!.Value,
                options.Partition.Strategy,
                projectPath,
                ct);

            // Show partition info
            _console.ForegroundColor = ConsoleColor.DarkGray;
            _console.WriteLine($"Partition {options.Partition.Index + 1} of {options.Partition.Total}: {partitionResult.Files.Count} file(s) of {partitionResult.TotalFiles} total");
            if (partitionResult.PartitionSpecs.HasValue)
            {
                _console.WriteLine($"  {partitionResult.PartitionSpecs} spec(s) of {partitionResult.TotalSpecs} total");
            }
            _console.ResetColor();
            _console.WriteLine();

            specFiles = partitionResult.Files;

            // Empty partition is success
            if (specFiles.Count == 0)
            {
                _console.WriteLine("No specs in this partition.");
                return 0;
            }
        }

        // Show pre-run stats (unless disabled)
        if (!options.NoStats || options.StatsOnly)
        {
            var stats = await _statsCollector.CollectAsync(specFiles, projectPath, ct);
            presenter.ShowPreRunStats(stats);

            // If --stats-only, just show stats and exit
            if (options.StatsOnly)
            {
                // Exit code 2 if focus mode is active (to signal unexpected state in CI)
                return stats.HasFocusMode ? 2 : 0;
            }
        }

        // Set up build event handlers
        runner.OnBuildStarted += presenter.ShowBuilding;
        runner.OnBuildCompleted += presenter.ShowBuildResult;

        // Run all specs
        var summary = await runner.RunAllAsync(specFiles, options.Parallel, ct);

        // Merge results into a combined report
        var combinedReport = MergeReports(summary.Results, Path.GetFullPath(options.Path));

        // Format output
        string output;
        if (options.Format == OutputFormat.Console)
        {
            // Console format - show directly
            ShowConsoleOutput(summary, options.Path, presenter);
            output = ""; // Already displayed
        }
        else if (options.Format == OutputFormat.Json)
        {
            output = combinedReport.ToJson();
        }
        else
        {
            var formatter = _formatterRegistry.GetFormatter(options.Format.ToCliString(), options.CssUrl)
                            ?? throw new ArgumentException($"Unknown format: {options.Format}");
            output = formatter.Format(combinedReport);
        }

        // Output to file or stdout
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            // Security: Validate output path is within current directory
            ValidateOutputPath(options.OutputFile);
            await _fileSystem.WriteAllTextAsync(options.OutputFile, output, ct);
            _console.WriteLine($"Report written to {options.OutputFile}");
        }
        else if (!string.IsNullOrEmpty(output))
        {
            _console.WriteLine(output);
        }

        return summary.Success ? 0 : 1;
    }

    private void ShowConsoleOutput(InProcessRunSummary summary, string basePath, ConsolePresenter presenter)
    {
        presenter.ShowHeader(summary.Results.Select(r => r.SpecFile).ToList(), false);
        presenter.ShowSpecsStarting();

        foreach (var result in summary.Results)
        {
            // Check for compilation errors with enhanced diagnostics
            if (result.Error is CompilationDiagnosticException compilationError)
            {
                presenter.ShowCompilationError(compilationError);
                continue;
            }

            // Convert to legacy format for presenter
            var legacyResult = new SpecRunResult(
                result.SpecFile,
                ConsoleOutputFormatter.Format(result.Report),
                result.Error?.Message ?? "",
                result.Success ? 0 : 1,
                result.Duration);

            presenter.ShowResult(legacyResult, basePath);
        }

        // Show summary
        var legacySummary = new RunSummary(
            summary.Results.Select(r => new SpecRunResult(
                r.SpecFile,
                "",
                r.Error?.Message ?? "",
                r.Success ? 0 : 1,
                r.Duration)).ToList(),
            summary.TotalDuration);

        presenter.ShowSummary(legacySummary);
    }

    private static SpecReport MergeReports(IReadOnlyList<InProcessRunResult> results, string source)
    {
        var combined = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Source = source,
            Summary = new SpecSummary
            {
                Total = results.Sum(r => r.Report.Summary.Total),
                Passed = results.Sum(r => r.Report.Summary.Passed),
                Failed = results.Sum(r => r.Report.Summary.Failed),
                Pending = results.Sum(r => r.Report.Summary.Pending),
                Skipped = results.Sum(r => r.Report.Summary.Skipped),
                DurationMs = results.Sum(r => r.Report.Summary.DurationMs)
            }
        };

        // Merge all contexts from all reports
        foreach (var result in results)
        {
            combined.Contexts.AddRange(result.Report.Contexts);
        }

        return combined;
    }

    private void ValidateOutputPath(string outputFile)
    {
        var outputFullPath = Path.GetFullPath(outputFile);
        var currentDir = _environment.CurrentDirectory;
        var normalizedBase = currentDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var outputDir = Path.GetDirectoryName(outputFullPath) ?? currentDir;
        var normalizedOutput = outputDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!normalizedOutput.StartsWith(normalizedBase, comparison))
            throw new SecurityException("Output file must be within current directory");
    }

    /// <summary>
    /// Builds a regex pattern to match specs at specified line numbers.
    /// Uses static parsing to discover spec structure and line numbers.
    /// </summary>
    private async Task<string?> BuildLineFilterPatternAsync(RunOptions options, CancellationToken ct)
    {
        var matchingDisplayNames = new List<string>();
        var projectPath = Path.GetFullPath(options.Path);

        // If path is a file, use its directory as project path
        if (_fileSystem.FileExists(projectPath))
            projectPath = Path.GetDirectoryName(projectPath)!;

        var parser = new StaticSpecParser(projectPath);

        foreach (var filter in options.Filter.LineFilters!)
        {
            var filePath = Path.GetFullPath(filter.File, projectPath);

            if (!_fileSystem.FileExists(filePath))
            {
                _console.WriteWarning($"File not found: {filter.File}");
                continue;
            }

            var result = await parser.ParseFileAsync(filePath, ct);

            // Find specs at the specified line numbers
            // Also find describe blocks - if a line matches a describe, include all its specs
            foreach (var lineNumber in filter.Lines)
            {
                // Check if any spec is at this line
                var matchingSpecs = result.Specs
                    .Where(s => s.LineNumber == lineNumber)
                    .ToList();

                if (matchingSpecs.Count > 0)
                {
                    foreach (var spec in matchingSpecs)
                    {
                        var displayName = GenerateDisplayName(spec.ContextPath, spec.Description);
                        matchingDisplayNames.Add(displayName);
                    }
                }
                else
                {
                    // No spec at exact line - check if line is within a context block
                    // Find all specs whose context path suggests they're in a describe at this line
                    // This is approximate - we match specs that have a context starting near this line
                    var nearbySpecs = result.Specs
                        .Where(s => Math.Abs(s.LineNumber - lineNumber) <= 1)
                        .ToList();

                    foreach (var spec in nearbySpecs)
                    {
                        var displayName = GenerateDisplayName(spec.ContextPath, spec.Description);
                        matchingDisplayNames.Add(displayName);
                    }
                }
            }
        }

        if (matchingDisplayNames.Count == 0)
            return null;

        // Build regex pattern that matches any of the display names exactly
        var escapedNames = matchingDisplayNames
            .Distinct()
            .Select(Regex.Escape);

        return $"^({string.Join("|", escapedNames)})$";
    }

    private static string GenerateDisplayName(IReadOnlyList<string> contextPath, string description)
    {
        if (contextPath.Count == 0)
            return description;

        return string.Join(" > ", contextPath) + " > " + description;
    }
}

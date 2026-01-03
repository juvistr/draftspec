using System.Security;
using System.Text.RegularExpressions;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyGraph;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.History;
using DraftSpec.Cli.Interactive;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
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
    private readonly IGitService _gitService;
    private readonly ISpecHistoryService _historyService;
    private readonly ISpecSelector _specSelector;

    public RunCommand(
        ISpecFinder specFinder,
        IInProcessSpecRunnerFactory runnerFactory,
        IConsole console,
        ICliFormatterRegistry formatterRegistry,
        IFileSystem fileSystem,
        IEnvironment environment,
        ISpecStatsCollector statsCollector,
        ISpecPartitioner partitioner,
        IGitService gitService,
        ISpecHistoryService historyService,
        ISpecSelector specSelector)
    {
        _specFinder = specFinder;
        _runnerFactory = runnerFactory;
        _console = console;
        _formatterRegistry = formatterRegistry;
        _fileSystem = fileSystem;
        _environment = environment;
        _statsCollector = statsCollector;
        _partitioner = partitioner;
        _gitService = gitService;
        _historyService = historyService;
        _specSelector = specSelector;
    }

    public async Task<int> ExecuteAsync(RunOptions options, CancellationToken ct = default)
    {
        // Get project path early for history operations
        var projectPath = Path.GetFullPath(options.Path);
        if (_fileSystem.FileExists(projectPath))
            projectPath = Path.GetDirectoryName(projectPath)!;

        // Apply quarantine filtering if specified
        var excludeName = options.Filter.ExcludeName;
        if (options.Quarantine)
        {
            var history = await _historyService.LoadAsync(projectPath, ct);
            var flakySpecs = _historyService.GetFlakySpecs(history);

            if (flakySpecs.Count > 0)
            {
                _console.ForegroundColor = ConsoleColor.DarkGray;
                _console.WriteLine($"Quarantining {flakySpecs.Count} flaky spec(s)");
                _console.ResetColor();

                // Build exclude pattern from flaky display names
                var flakyPattern = string.Join("|", flakySpecs.Select(f => Regex.Escape(f.DisplayName)));
                excludeName = string.IsNullOrEmpty(excludeName)
                    ? $"^({flakyPattern})$"
                    : $"({excludeName})|^({flakyPattern})$";
            }
        }

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

        // Find spec files
        var specFiles = _specFinder.FindSpecs(options.Path);
        if (specFiles.Count == 0)
        {
            _console.WriteLine("No spec files found.");
            return 0;
        }

        // Apply test impact analysis filtering if specified
        if (!string.IsNullOrEmpty(options.AffectedBy))
        {
            var impactResult = await ApplyImpactAnalysisAsync(specFiles, options, ct);
            if (impactResult.ShouldExit)
                return impactResult.ExitCode;
            specFiles = impactResult.FilteredFiles;
        }

        // Apply interactive selection if enabled
        if (options.Interactive)
        {
            var interactiveResult = await ApplyInteractiveSelectionAsync(specFiles, options, ct);
            if (interactiveResult.ShouldExit)
                return interactiveResult.ExitCode;

            // Apply selected filter pattern
            filterName = string.IsNullOrEmpty(filterName)
                ? interactiveResult.FilterPattern
                : $"({filterName})|({interactiveResult.FilterPattern})";
        }

        // Create runner with final filter values
        var runner = _runnerFactory.Create(
            options.Filter.FilterTags,
            options.Filter.ExcludeTags,
            filterName,
            excludeName,
            options.Filter.FilterContext,
            options.Filter.ExcludeContext);

        // Set up presenter for console output
        var presenter = new ConsolePresenter(_console, watchMode: false);

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

        // Record results to history (unless disabled)
        if (!options.NoHistory)
        {
            var records = ExtractRunRecords(summary.Results, projectPath);
            if (records.Count > 0)
            {
                await _historyService.RecordRunAsync(projectPath, records, ct);
            }
        }

        return summary.Success ? 0 : 1;
    }

    /// <summary>
    /// Extracts spec run records from run results for history tracking.
    /// </summary>
    private static List<SpecRunRecord> ExtractRunRecords(
        IReadOnlyList<InProcessRunResult> results,
        string projectPath)
    {
        var records = new List<SpecRunRecord>();

        foreach (var result in results)
        {
            var relativePath = Path.GetRelativePath(projectPath, result.SpecFile);

            // Traverse the context tree to extract all specs
            foreach (var context in result.Report.Contexts)
            {
                ExtractSpecsFromContext(context, [], relativePath, records);
            }
        }

        return records;
    }

    /// <summary>
    /// Recursively extracts specs from a context and its children.
    /// </summary>
    private static void ExtractSpecsFromContext(
        SpecContextReport context,
        List<string> contextPath,
        string relativePath,
        List<SpecRunRecord> records)
    {
        // Add current context to path
        var currentPath = new List<string>(contextPath) { context.Description };

        // Extract specs in this context
        foreach (var spec in context.Specs)
        {
            var specId = GenerateSpecId(relativePath, currentPath, spec.Description);
            var displayName = GenerateDisplayName(currentPath, spec.Description);

            records.Add(new SpecRunRecord
            {
                SpecId = specId,
                DisplayName = displayName,
                Status = spec.Status.ToLowerInvariant(),
                DurationMs = spec.DurationMs ?? 0,
                ErrorMessage = spec.Error
            });
        }

        // Recurse into child contexts
        foreach (var child in context.Contexts)
        {
            ExtractSpecsFromContext(child, currentPath, relativePath, records);
        }
    }

    /// <summary>
    /// Generates a stable spec ID from file path, context path, and description.
    /// </summary>
    private static string GenerateSpecId(
        string relativePath,
        IReadOnlyList<string> contextPath,
        string description)
    {
        var path = string.Join("/", contextPath);
        return $"{relativePath}:{path}/{description}";
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
            foreach (var context in result.Report.Contexts)
            {
                combined.Contexts.Add(context);
            }
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

        var parser = new StaticSpecParser(projectPath, useCache: !options.NoCache);

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

    /// <summary>
    /// Applies test impact analysis to filter spec files based on changed source files.
    /// </summary>
    private async Task<ImpactAnalysisResult> ApplyImpactAnalysisAsync(
        IReadOnlyList<string> specFiles,
        RunOptions options,
        CancellationToken ct)
    {
        var projectPath = Path.GetFullPath(options.Path);
        if (_fileSystem.FileExists(projectPath))
            projectPath = Path.GetDirectoryName(projectPath)!;

        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.WriteLine($"Analyzing impact of changes: {options.AffectedBy}");
        _console.ResetColor();

        // Get changed files from git
        IReadOnlyList<string> changedFiles;
        try
        {
            changedFiles = await _gitService.GetChangedFilesAsync(options.AffectedBy!, projectPath, ct);
        }
        catch (InvalidOperationException ex)
        {
            _console.WriteError($"Failed to get changed files: {ex.Message}");
            return new ImpactAnalysisResult([], ShouldExit: true, ExitCode: 1);
        }

        if (changedFiles.Count == 0)
        {
            _console.WriteLine("No changed files detected.");
            return new ImpactAnalysisResult([], ShouldExit: true, ExitCode: 0);
        }

        // Build dependency graph
        var graphBuilder = new DependencyGraphBuilder();
        var graph = await graphBuilder.BuildAsync(projectPath, cancellationToken: ct);

        // Get affected specs
        var affectedSpecs = graph.GetAffectedSpecs(changedFiles);

        // Filter to only specs that exist in our discovered spec files
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var specFileSet = new HashSet<string>(specFiles, pathComparer);
        var filteredSpecs = affectedSpecs
            .Where(s => specFileSet.Contains(s))
            .ToList();

        // Show impact analysis summary
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.WriteLine($"Changed files: {changedFiles.Count}");
        _console.WriteLine($"Affected specs: {filteredSpecs.Count} of {specFiles.Count}");
        _console.ResetColor();
        _console.WriteLine();

        // If dry run, output affected specs and exit
        if (options.DryRun)
        {
            _console.WriteLine("Affected spec files (dry run):");
            foreach (var spec in filteredSpecs.OrderBy(s => s))
            {
                _console.WriteLine($"  {Path.GetRelativePath(projectPath, spec)}");
            }
            return new ImpactAnalysisResult(filteredSpecs, ShouldExit: true, ExitCode: 0);
        }

        if (filteredSpecs.Count == 0)
        {
            _console.WriteLine("No affected specs to run.");
            return new ImpactAnalysisResult([], ShouldExit: true, ExitCode: 0);
        }

        return new ImpactAnalysisResult(filteredSpecs, ShouldExit: false, ExitCode: 0);
    }

    private record ImpactAnalysisResult(
        IReadOnlyList<string> FilteredFiles,
        bool ShouldExit,
        int ExitCode);

    /// <summary>
    /// Applies interactive spec selection, allowing the user to choose which specs to run.
    /// </summary>
    private async Task<InteractiveSelectionResult> ApplyInteractiveSelectionAsync(
        IReadOnlyList<string> specFiles,
        RunOptions options,
        CancellationToken ct)
    {
        var projectPath = Path.GetFullPath(options.Path);
        if (_fileSystem.FileExists(projectPath))
            projectPath = Path.GetDirectoryName(projectPath)!;

        // Discover all specs using static parser (fast, no execution)
        var parser = new StaticSpecParser(projectPath, useCache: !options.NoCache);
        var allSpecs = new List<DiscoveredSpec>();

        foreach (var specFile in specFiles)
        {
            try
            {
                var result = await parser.ParseFileAsync(specFile, ct);
                var relativePath = Path.GetRelativePath(projectPath, specFile);

                foreach (var staticSpec in result.Specs)
                {
                    var id = GenerateSpecId(relativePath, staticSpec.ContextPath, staticSpec.Description);
                    var displayName = GenerateDisplayName(staticSpec.ContextPath, staticSpec.Description);

                    allSpecs.Add(new DiscoveredSpec
                    {
                        Id = id,
                        Description = staticSpec.Description,
                        DisplayName = displayName,
                        ContextPath = staticSpec.ContextPath,
                        SourceFile = specFile,
                        RelativeSourceFile = relativePath,
                        LineNumber = staticSpec.LineNumber,
                        IsPending = staticSpec.IsPending,
                        IsSkipped = staticSpec.Type == StaticSpecType.Skipped,
                        IsFocused = staticSpec.Type == StaticSpecType.Focused,
                        Tags = []
                    });
                }
            }
            catch (Exception ex)
            {
                _console.WriteWarning($"Failed to parse {Path.GetFileName(specFile)}: {ex.Message}");
            }
        }

        if (allSpecs.Count == 0)
        {
            _console.WriteLine("No specs found for interactive selection.");
            return new InteractiveSelectionResult(ShouldExit: true, ExitCode: 0, FilterPattern: null);
        }

        // Show interactive selector
        SpecSelectionResult selectionResult;
        try
        {
            selectionResult = await _specSelector.SelectAsync(allSpecs, ct);
        }
        catch (InvalidOperationException ex)
        {
            // Non-interactive terminal or other error
            _console.WriteError(ex.Message);
            return new InteractiveSelectionResult(ShouldExit: true, ExitCode: 1, FilterPattern: null);
        }

        if (selectionResult.Cancelled)
        {
            _console.WriteLine("Selection cancelled.");
            return new InteractiveSelectionResult(ShouldExit: true, ExitCode: 0, FilterPattern: null);
        }

        if (selectionResult.SelectedSpecIds.Count == 0)
        {
            _console.WriteLine("No specs selected.");
            return new InteractiveSelectionResult(ShouldExit: true, ExitCode: 0, FilterPattern: null);
        }

        // Show selection summary
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.WriteLine($"Selected {selectionResult.SelectedSpecIds.Count} of {selectionResult.TotalCount} specs");
        _console.ResetColor();
        _console.WriteLine();

        // Build filter pattern from selected display names
        var escapedNames = selectionResult.SelectedDisplayNames
            .Select(Regex.Escape);
        var filterPattern = $"^({string.Join("|", escapedNames)})$";

        return new InteractiveSelectionResult(ShouldExit: false, ExitCode: 0, FilterPattern: filterPattern);
    }

    private record InteractiveSelectionResult(bool ShouldExit, int ExitCode, string? FilterPattern);
}

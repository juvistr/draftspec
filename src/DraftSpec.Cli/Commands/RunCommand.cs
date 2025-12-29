using System.Security;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.Services;
using DraftSpec.Formatters;

namespace DraftSpec.Cli.Commands;

public class RunCommand : ICommand
{
    private readonly ISpecFinder _specFinder;
    private readonly IInProcessSpecRunnerFactory _runnerFactory;
    private readonly IConsole _console;
    private readonly ICliFormatterRegistry _formatterRegistry;
    private readonly IConfigLoader _configLoader;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironment _environment;

    public RunCommand(
        ISpecFinder specFinder,
        IInProcessSpecRunnerFactory runnerFactory,
        IConsole console,
        ICliFormatterRegistry formatterRegistry,
        IConfigLoader configLoader,
        IFileSystem fileSystem,
        IEnvironment environment)
    {
        _specFinder = specFinder;
        _runnerFactory = runnerFactory;
        _console = console;
        _formatterRegistry = formatterRegistry;
        _configLoader = configLoader;
        _fileSystem = fileSystem;
        _environment = environment;
    }

    public async Task<int> ExecuteAsync(CliOptions options, CancellationToken ct = default)
    {
        // Load project configuration from draftspec.json
        var configResult = _configLoader.Load(options.Path);
        if (configResult.Error != null)
            throw new InvalidOperationException(configResult.Error);

        if (configResult.Config != null)
            options.ApplyDefaults(configResult.Config);

        var runner = _runnerFactory.Create(
            options.FilterTags,
            options.ExcludeTags,
            options.FilterName,
            options.ExcludeName);

        var specFiles = _specFinder.FindSpecs(options.Path);
        if (specFiles.Count == 0)
        {
            _console.WriteLine("No spec files found.");
            return 0;
        }

        // Set up build event handlers for console output
        var presenter = new ConsolePresenter(_console, watchMode: false);
        runner.OnBuildStarted += presenter.ShowBuilding;
        runner.OnBuildCompleted += presenter.ShowBuildResult;

        // Run all specs
        var summary = await runner.RunAllAsync(specFiles, options.Parallel, ct);

        // Merge results into a combined report
        var combinedReport = MergeReports(summary.Results, Path.GetFullPath(options.Path));

        // Format output
        string output;
        if (options.Format == OutputFormats.Console)
        {
            // Console format - show directly
            ShowConsoleOutput(summary, options.Path, presenter);
            output = ""; // Already displayed
        }
        else if (options.Format == OutputFormats.Json)
        {
            output = combinedReport.ToJson();
        }
        else
        {
            var formatter = _formatterRegistry.GetFormatter(options.Format, options)
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
}

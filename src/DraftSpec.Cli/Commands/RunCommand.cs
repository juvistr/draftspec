using System.Security;
using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.Coverage;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;

namespace DraftSpec.Cli.Commands;

public static class RunCommand
{
    /// <summary>
    /// Get a formatter by name using the provided registry.
    /// Falls back to built-in formatters if registry is null.
    /// </summary>
    public static IFormatter? GetFormatter(string name, CliOptions options, ICliFormatterRegistry? registry = null)
    {
        registry ??= new CliFormatterRegistry();
        return registry.GetFormatter(name, options);
    }

    public static int Execute(CliOptions options, ICliFormatterRegistry? formatterRegistry = null)
    {
        // Load project configuration from draftspec.json
        var configResult = ConfigLoader.Load(options.Path);
        if (configResult.Error != null)
        {
            Console.Error.WriteLine($"Error: {configResult.Error}");
            return 1;
        }

        if (configResult.Config != null)
            options.ApplyDefaults(configResult.Config);

        // Initialize coverage runner if enabled
        CoverageRunner? coverageRunner = null;
        if (options.Coverage)
        {
            if (!CoverageToolDetector.IsAvailable)
            {
                Console.Error.WriteLine("Error: dotnet-coverage tool is not installed.");
                Console.Error.WriteLine("Install with: dotnet tool install -g dotnet-coverage");
                return 1;
            }

            var coverageOutput = Path.GetFullPath(options.CoverageOutput ?? "./coverage");
            coverageRunner = new CoverageRunner(coverageOutput, options.CoverageFormat);
        }

        var finder = new SpecFinder();
        var runner = new SpecFileRunner(
            options.NoCache,
            options.FilterTags,
            options.ExcludeTags,
            options.FilterName,
            options.ExcludeName,
            coverageRunner);

        // For non-console formats, we need JSON output from specs
        var needsJson = options.Format is OutputFormats.Json or OutputFormats.Markdown or OutputFormats.Html;
        bool hasFailures;

        if (!needsJson)
        {
            // Console output - use existing presenter
            var presenter = new ConsolePresenter(false);
            runner.OnBuildStarted += presenter.ShowBuilding;
            runner.OnBuildCompleted += presenter.ShowBuildResult;

            var specFiles = finder.FindSpecs(options.Path);
            presenter.ShowHeader(specFiles, options.Parallel);

            var summary = runner.RunAll(specFiles, options.Parallel);

            presenter.ShowSpecsStarting();
            foreach (var result in summary.Results) presenter.ShowResult(result, options.Path);

            presenter.ShowSummary(summary);
            hasFailures = !summary.Success;
        }
        else
        {
            // JSON/Markdown/HTML - run with JSON output and format
            var specFilesForJson = finder.FindSpecs(options.Path);

            // Run specs with JSON output via FileReporter (builds projects automatically)
            var jsonOutputs = new List<string>();
            hasFailures = false;

            foreach (var specFile in specFilesForJson)
            {
                var result = runner.RunWithJsonReporter(specFile);
                if (!string.IsNullOrWhiteSpace(result.Output))
                    // Output is now clean JSON from file (not mixed with console output)
                    jsonOutputs.Add(result.Output);
                if (!result.Success) hasFailures = true;
            }

            // Merge all JSON reports into a combined report
            var combinedReport = ReportMerger.Merge(jsonOutputs, Path.GetFullPath(options.Path));

            string output;
            if (options.Format == OutputFormats.Json)
            {
                output = combinedReport.ToJson();
            }
            else
            {
                var formatter = GetFormatter(options.Format, options, formatterRegistry)
                                ?? throw new ArgumentException($"Unknown format: {options.Format}");
                output = formatter.Format(combinedReport);
            }

            // Output to file or stdout
            if (!string.IsNullOrEmpty(options.OutputFile))
            {
                // Security: Validate output path is within current directory
                // Uses same pattern as SpecFinder: trailing separator + platform-aware comparison
                var outputFullPath = Path.GetFullPath(options.OutputFile);
                var currentDir = Directory.GetCurrentDirectory();
                var normalizedBase = currentDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var outputDir = Path.GetDirectoryName(outputFullPath) ?? currentDir;
                var normalizedOutput = outputDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                // Use platform-appropriate case sensitivity
                var comparison = OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

                if (!normalizedOutput.StartsWith(normalizedBase, comparison))
                    // Generic error - don't expose internal directory structure
                    throw new SecurityException("Output file must be within current directory");

                File.WriteAllText(outputFullPath, output);
                Console.WriteLine($"Report written to {options.OutputFile}");
            }
            else
            {
                Console.WriteLine(output);
            }
        }

        // Merge and report coverage if enabled
        var coverageExitCode = HandleCoverage(coverageRunner, configResult.Config, options);
        if (coverageExitCode.HasValue)
            return coverageExitCode.Value;

        return hasFailures ? 1 : 0;
    }

    /// <summary>
    /// Handle coverage merging, reporting, and threshold checking.
    /// Returns an exit code if coverage threshold failed, null otherwise.
    /// </summary>
    private static int? HandleCoverage(CoverageRunner? coverageRunner, DraftSpecProjectConfig? config, CliOptions options)
    {
        if (coverageRunner == null)
            return null;

        var mergedFile = coverageRunner.MergeCoverageFiles();
        if (mergedFile == null)
            return null;

        Console.WriteLine();
        Console.WriteLine($"Coverage report: {mergedFile}");
        coverageRunner.CleanupIntermediateFiles();

        // Generate additional report formats if requested (CLI option takes precedence)
        var reportFormatsStr = options.CoverageReportFormats;
        var reportFormats = !string.IsNullOrEmpty(reportFormatsStr)
            ? reportFormatsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : config?.Coverage?.ReportFormats;

        if (reportFormats is { Count: > 0 })
        {
            var generatedReports = coverageRunner.GenerateReports(mergedFile, reportFormats);
            foreach (var (format, path) in generatedReports)
            {
                Console.WriteLine($"Coverage {format} report: {path}");
            }
        }

        // Check thresholds if configured
        var thresholds = config?.Coverage?.Thresholds;
        if (thresholds != null && (thresholds.Line.HasValue || thresholds.Branch.HasValue))
        {
            var checker = new CoverageThresholdChecker();
            var result = checker.CheckFile(mergedFile, thresholds);

            if (result != null)
            {
                Console.WriteLine($"Coverage: {result.ActualLinePercent:F1}% lines, {result.ActualBranchPercent:F1}% branches");

                if (!result.Passed)
                {
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("Coverage threshold check failed:");
                    foreach (var failure in result.Failures)
                        Console.Error.WriteLine($"  {failure}");
                    return 2; // Exit code 2 for coverage threshold failure
                }
            }
        }

        return null;
    }
}
using System.Security;
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
        var finder = new SpecFinder();
        var runner = new SpecFileRunner();

        // For non-console formats, we need JSON output from specs
        var needsJson = options.Format is OutputFormats.Json or OutputFormats.Markdown or OutputFormats.Html;

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
            return summary.Success ? 0 : 1;
        }

        // JSON/Markdown/HTML - run with JSON output and format
        var specFilesForJson = finder.FindSpecs(options.Path);

        // Run specs with JSON output via FileReporter (builds projects automatically)
        var jsonOutputs = new List<string>();
        var hasFailures = false;

        foreach (var specFile in specFilesForJson)
        {
            var result = runner.RunWithJsonReporter(specFile);
            if (!string.IsNullOrWhiteSpace(result.Output))
                // Output is now clean JSON from file (not mixed with console output)
                jsonOutputs.Add(result.Output);
            if (!result.Success) hasFailures = true;
        }

        // Merge all JSON reports into a combined report
        var combinedReport = MergeReports(jsonOutputs, Path.GetFullPath(options.Path));

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

        return hasFailures ? 1 : 0;
    }

    /// <summary>
    /// Merge multiple JSON reports into a single combined report.
    /// </summary>
    private static SpecReport MergeReports(List<string> jsonOutputs, string source)
    {
        if (jsonOutputs.Count == 0)
            return new SpecReport
            {
                Timestamp = DateTime.UtcNow,
                Source = source,
                Summary = new SpecSummary(),
                Contexts = []
            };

        // Parse all reports
        var reports = jsonOutputs
            .Where(json => !string.IsNullOrWhiteSpace(json))
            .Select(SpecReport.FromJson)
            .ToList();

        if (reports.Count == 0)
            return new SpecReport
            {
                Timestamp = DateTime.UtcNow,
                Source = source,
                Summary = new SpecSummary(),
                Contexts = []
            };

        // Single report - just update source
        if (reports.Count == 1)
        {
            reports[0].Source = source;
            return reports[0];
        }

        // Merge multiple reports
        var combined = new SpecReport
        {
            Timestamp = reports.Min(r => r.Timestamp),
            Source = source,
            Contexts = reports.SelectMany(r => r.Contexts).ToList(),
            Summary = new SpecSummary
            {
                Total = reports.Sum(r => r.Summary.Total),
                Passed = reports.Sum(r => r.Summary.Passed),
                Failed = reports.Sum(r => r.Summary.Failed),
                Pending = reports.Sum(r => r.Summary.Pending),
                Skipped = reports.Sum(r => r.Summary.Skipped),
                DurationMs = reports.Sum(r => r.Summary.DurationMs)
            }
        };

        return combined;
    }
}
using DraftSpec.Formatters;
using DraftSpec.Formatters.Html;
using DraftSpec.Formatters.Markdown;

namespace DraftSpec.Cli.Commands;

public static class RunCommand
{
    private static readonly Dictionary<string, IFormatter> Formatters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["json"] = new JsonFormatter(),
        ["markdown"] = new MarkdownFormatter()
    };

    /// <summary>
    /// Get a formatter by name, with support for built-in formatters.
    /// </summary>
    public static IFormatter? GetFormatter(string name, CliOptions options)
    {
        if (name.Equals("html", StringComparison.OrdinalIgnoreCase))
        {
            return new HtmlFormatter(new HtmlOptions
            {
                CssUrl = options.CssUrl ?? "https://cdnjs.cloudflare.com/ajax/libs/simpledotcss/2.3.7/simple.min.css"
            });
        }
        return Formatters.GetValueOrDefault(name);
    }

    public static int Execute(CliOptions options)
    {
        var finder = new SpecFinder();
        var runner = new SpecFileRunner();

        // For non-console formats, we need JSON output from specs
        var needsJson = options.Format is "json" or "markdown" or "html";

        if (!needsJson)
        {
            // Console output - use existing presenter
            var presenter = new ConsolePresenter(watchMode: false);
            runner.OnBuildStarted += presenter.ShowBuilding;
            runner.OnBuildCompleted += presenter.ShowBuildResult;

            var specFiles = finder.FindSpecs(options.Path);
            presenter.ShowHeader(specFiles, options.Parallel);

            var summary = runner.RunAll(specFiles, options.Parallel);

            presenter.ShowSpecsStarting();
            foreach (var result in summary.Results)
            {
                presenter.ShowResult(result, options.Path);
            }

            presenter.ShowSummary(summary);
            return summary.Success ? 0 : 1;
        }

        // JSON/Markdown/HTML - run with JSON output and format
        var specFilesForJson = finder.FindSpecs(options.Path);

        // Build projects first (silently for formatted output)
        foreach (var dir in specFilesForJson.Select(f => Path.GetDirectoryName(Path.GetFullPath(f))!).Distinct())
        {
            BuildProjects(dir);
        }

        // Run specs with JSON output and collect
        var jsonOutputs = new List<string>();
        var hasFailures = false;

        foreach (var specFile in specFilesForJson)
        {
            var result = runner.RunWithJson(specFile);
            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                jsonOutputs.Add(result.Output);
            }
            if (!result.Success)
            {
                hasFailures = true;
            }
        }

        // Combine JSON reports (for now, just use first one if single file)
        // TODO: Proper merging for multiple spec files
        var jsonReport = jsonOutputs.FirstOrDefault() ?? "{}";

        string output;
        if (options.Format == "json")
        {
            output = jsonReport;
        }
        else
        {
            var report = SpecReport.FromJson(jsonReport);
            report.Source = Path.GetFullPath(options.Path);
            var formatter = GetFormatter(options.Format, options)
                ?? throw new ArgumentException($"Unknown format: {options.Format}");
            output = formatter.Format(report);
        }

        // Output to file or stdout
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            File.WriteAllText(options.OutputFile, output);
            Console.WriteLine($"Report written to {options.OutputFile}");
        }
        else
        {
            Console.WriteLine(output);
        }

        return hasFailures ? 1 : 0;
    }

    private static void BuildProjects(string directory)
    {
        var projects = Directory.GetFiles(directory, "*.csproj");
        foreach (var project in projects)
        {
            ProcessHelper.RunDotnet(["build", project, "--nologo", "-v", "q"], directory);
        }
    }
}

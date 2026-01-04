using DraftSpec.Abstractions;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Renders and outputs run results.
/// Supports console, JSON, HTML, and Markdown output formats.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[RunResults]</c></para>
/// <para><b>Optional:</b> <c>Items[OutputFormat]</c>, <c>Items[OutputFile]</c>, <c>Items[StatsOnly]</c>, <c>Items[NoStats]</c></para>
/// <para><b>Returns:</b> Exit code based on test results (0 = success, 1 = failures)</para>
/// </remarks>
public class RunOutputPhase : ICommandPhase
{
    private readonly ICliFormatterRegistry _formatterRegistry;
    private readonly IPathValidator _pathValidator;

    public RunOutputPhase(
        ICliFormatterRegistry formatterRegistry,
        IPathValidator pathValidator)
    {
        _formatterRegistry = formatterRegistry;
        _pathValidator = pathValidator;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var results = context.Get<InProcessRunSummary>(ContextKeys.RunResults);
        if (results == null)
            return await pipeline(context, ct);

        var outputFormat = context.Get<string>(ContextKeys.OutputFormat) ?? "console";
        var outputFile = context.Get<string>(ContextKeys.OutputFile);
        var projectPath = context.Get<string>(ContextKeys.ProjectPath) ?? ".";
        var statsOnly = context.Get<bool>(ContextKeys.StatsOnly);
        var noStats = context.Get<bool>(ContextKeys.NoStats);

        // Merge all reports
        var report = MergeReports(results, projectPath);

        // Handle output
        if (string.Equals(outputFormat, "console", StringComparison.Ordinal))
        {
            ShowConsoleSummary(context.Console, results, statsOnly, noStats);
        }
        else
        {
            var formatter = _formatterRegistry.GetFormatter(outputFormat);
            if (formatter == null)
            {
                context.Console.WriteError($"Unknown format: {outputFormat}");
                return 1;
            }

            var output = formatter.Format(report);
            if (string.IsNullOrEmpty(outputFile))
            {
                context.Console.WriteLine(output);
            }
            else
            {
                var fullPath = Path.GetFullPath(outputFile, projectPath);
                _pathValidator.ValidatePathWithinBase(fullPath, projectPath);
                await context.FileSystem.WriteAllTextAsync(fullPath, output, ct);
                context.Console.WriteLine($"Output written to: {outputFile}");
            }
        }

        // Continue pipeline, then return exit code based on results
        var pipelineResult = await pipeline(context, ct);
        if (pipelineResult != 0)
            return pipelineResult;

        return results.Success ? 0 : 1;
    }

    private static void ShowConsoleSummary(
        IConsole console,
        InProcessRunSummary summary,
        bool statsOnly,
        bool noStats)
    {
        console.WriteLine();

        // Show file results unless stats only
        if (!statsOnly)
        {
            foreach (var result in summary.Results)
            {
                var status = result.Success ? "PASS" : "FAIL";
                var color = result.Success ? ConsoleColor.Green : ConsoleColor.Red;

                console.ForegroundColor = color;
                console.Write($"[{status}] ");
                console.ResetColor();
                console.WriteLine(Path.GetFileName(result.SpecFile));

                if (result.Error != null)
                {
                    console.ForegroundColor = ConsoleColor.Red;
                    console.WriteLine($"  Error: {result.Error.Message}");
                    console.ResetColor();
                }
            }

            console.WriteLine();
        }

        // Show summary unless no stats
        if (!noStats)
        {
            var summaryColor = summary.Success ? ConsoleColor.Green : ConsoleColor.Red;
            console.ForegroundColor = summaryColor;
            console.WriteLine($"Tests: {summary.TotalSpecs} total, " +
                $"{summary.Passed} passed, " +
                $"{summary.Failed} failed, " +
                $"{summary.Pending} pending, " +
                $"{summary.Skipped} skipped");
            console.WriteLine($"Duration: {summary.TotalDuration.TotalSeconds:F2}s");
            console.ResetColor();
        }
    }

    private static SpecReport MergeReports(InProcessRunSummary summary, string source)
    {
        var combined = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Source = source,
            Summary = new SpecSummary
            {
                Total = summary.Results.Sum(r => r.Report.Summary.Total),
                Passed = summary.Results.Sum(r => r.Report.Summary.Passed),
                Failed = summary.Results.Sum(r => r.Report.Summary.Failed),
                Pending = summary.Results.Sum(r => r.Report.Summary.Pending),
                Skipped = summary.Results.Sum(r => r.Report.Summary.Skipped),
                DurationMs = summary.Results.Sum(r => r.Report.Summary.DurationMs)
            }
        };

        // Merge all contexts from all reports
        foreach (var result in summary.Results)
        {
            foreach (var context in result.Report.Contexts)
            {
                combined.Contexts.Add(context);
            }
        }

        return combined;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using DraftSpec.Configuration;
using DraftSpec.Formatters;

namespace DraftSpec.Internal;

/// <summary>
/// Executes specs and produces formatted output.
/// Extracted from Dsl.run() for testability.
/// </summary>
internal static class SpecExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Execute specs and return the report.
    /// </summary>
    public static SpecReport Execute(SpecContext rootContext, SpecRunnerBuilder? builder = null)
    {
        var runner = builder?.Build() ?? new SpecRunner();
        var results = runner.Run(rootContext);
        return SpecReportBuilder.Build(rootContext, results);
    }

    /// <summary>
    /// Execute specs and write output to console.
    /// </summary>
    public static void ExecuteAndOutput(
        SpecContext? rootContext,
        bool json,
        Action resetState,
        SpecRunnerBuilder? builder = null,
        DraftSpecConfiguration? configuration = null)
    {
        // Check if we're in CLI mode with file-based JSON output
        var jsonOutputFile = Environment.GetEnvironmentVariable(Dsl.JsonOutputFileEnvVar);
        var useFileReporter = !string.IsNullOrEmpty(jsonOutputFile);

        if (rootContext is null)
        {
            if (json && !useFileReporter)
                Console.WriteLine("{}");
            else if (!useFileReporter)
                Console.WriteLine("No specs defined.");
            return;
        }

        var report = Execute(rootContext, builder);

        // Invoke reporters' OnRunCompletedAsync (includes FileReporter if configured)
        if (configuration != null)
            foreach (var reporter in configuration.Reporters.All)
                reporter.OnRunCompletedAsync(report).GetAwaiter().GetResult();

        // Output to console (skip JSON if using file reporter - it's already written)
        if (useFileReporter)
            // In file reporter mode, show console formatted output for user feedback
            FormatToConsole(configuration, report);
        else if (json)
            Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
        else
            FormatToConsole(configuration, report);

        // Set exit code based on failures
        if (report.Summary.Failed > 0) Environment.ExitCode = 1;

        // Reset state for next run
        resetState();
    }

    /// <summary>
    /// Format report to console using configured formatter or simple fallback.
    /// </summary>
    private static void FormatToConsole(DraftSpecConfiguration? configuration, SpecReport report)
    {
        var formatter = configuration?.ConsoleFormatter;
        if (formatter != null)
            formatter.Format(report, Console.Out);
        else
            // Simple fallback when no formatter is configured
            WritePlainTextSummary(report, Console.Out);
    }

    /// <summary>
    /// Write a simple plain-text summary when no console formatter is configured.
    /// </summary>
    private static void WritePlainTextSummary(SpecReport report, TextWriter output)
    {
        foreach (var context in report.Contexts) WriteContext(context, output, 0);

        output.WriteLine();
        output.Write($"{report.Summary.Total} specs: ");

        var parts = new List<string>();
        if (report.Summary.Passed > 0) parts.Add($"{report.Summary.Passed} passed");
        if (report.Summary.Failed > 0) parts.Add($"{report.Summary.Failed} failed");
        if (report.Summary.Pending > 0) parts.Add($"{report.Summary.Pending} pending");
        if (report.Summary.Skipped > 0) parts.Add($"{report.Summary.Skipped} skipped");
        output.WriteLine(string.Join(", ", parts));
    }

    private static void WriteContext(SpecContextReport context, TextWriter output, int level)
    {
        var indent = new string(' ', level * 2);
        output.WriteLine($"{indent}{context.Description}");

        foreach (var spec in context.Specs)
        {
            var status = spec.Status switch
            {
                "passed" => "✓",
                "failed" => "✗",
                "pending" => "○",
                "skipped" => "-",
                _ => "?"
            };
            output.WriteLine($"{indent}  {status} {spec.Description}");

            if (!string.IsNullOrEmpty(spec.Error)) output.WriteLine($"{indent}    {spec.Error}");
        }

        foreach (var child in context.Contexts) WriteContext(child, output, level + 1);
    }
}
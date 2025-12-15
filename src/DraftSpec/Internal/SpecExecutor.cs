using System.Text.Json;
using System.Text.Json.Serialization;
using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Console;

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
        {
            foreach (var reporter in configuration.Reporters.All)
            {
                reporter.OnRunCompletedAsync(report).GetAwaiter().GetResult();
            }
        }

        // Output to console (skip JSON if using file reporter - it's already written)
        if (useFileReporter)
        {
            // In file reporter mode, show console formatted output for user feedback
            var formatter = new ConsoleFormatter();
            formatter.Format(report, Console.Out);
        }
        else if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
        }
        else
        {
            var formatter = new ConsoleFormatter();
            formatter.Format(report, Console.Out);
        }

        // Set exit code based on failures
        if (report.Summary.Failed > 0)
        {
            Environment.ExitCode = 1;
        }

        // Reset state for next run
        resetState();
    }
}

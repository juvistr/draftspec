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
        if (rootContext is null)
        {
            if (json)
                Console.WriteLine("{}");
            else
                Console.WriteLine("No specs defined.");
            return;
        }

        var report = Execute(rootContext, builder);

        // Invoke reporters' OnRunCompletedAsync
        if (configuration != null)
        {
            foreach (var reporter in configuration.Reporters.All)
            {
                reporter.OnRunCompletedAsync(report).GetAwaiter().GetResult();
            }
        }

        if (json)
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

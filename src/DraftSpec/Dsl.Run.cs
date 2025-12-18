using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Internal;
using DraftSpec.Plugins.Reporters;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Environment variable name for CLI-based JSON output file path.
    /// When set, run() automatically adds a FileReporter to write JSON to this file.
    /// </summary>
    public const string JsonOutputFileEnvVar = "DRAFTSPEC_JSON_OUTPUT_FILE";

    /// <summary>
    /// Environment variable name for enabling progress streaming.
    /// When set to "true" or "1", run() adds a ProgressStreamReporter for MCP integration.
    /// </summary>
    public const string ProgressStreamEnvVar = "DRAFTSPEC_PROGRESS_STREAM";

    /// <summary>
    /// Run all collected specs and output results.
    /// Sets Environment.ExitCode to 1 if any specs failed.
    /// </summary>
    /// <param name="json">If true, output JSON instead of console format</param>
    public static void run(bool json = false)
    {
        var config = Configuration ?? new DraftSpecConfiguration();

        // Check for environment-based JSON output (CLI mode)
        var jsonOutputFile = Environment.GetEnvironmentVariable(JsonOutputFileEnvVar);
        if (!string.IsNullOrEmpty(jsonOutputFile))
        {
            // Add FileReporter for JSON output to the specified file
            var reporter = new FileReporter(jsonOutputFile, new JsonFormatter(),
                Path.GetTempPath());
            config.AddReporter(reporter);
        }

        // Check for progress streaming (MCP mode)
        var progressStream = Environment.GetEnvironmentVariable(ProgressStreamEnvVar);
        if (progressStream is "true" or "1")
        {
            config.AddReporter(new ProgressStreamReporter());
        }

        Configuration = config;

        SpecExecutor.ExecuteAndOutput(RootContext, json, ResetState, RunnerBuilder, Configuration);
    }

    private static void ResetState()
    {
        RootContext = null;
        CurrentContext = null;
        RunnerBuilder = null;
        Configuration?.Dispose();
        Configuration = null;
    }

    /// <summary>
    /// Reset all DSL state for a clean execution context.
    /// Used primarily for in-process execution modes.
    /// </summary>
    public static void Reset()
    {
        ResetState();
    }
}
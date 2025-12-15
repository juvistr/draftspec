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
    /// Run all collected specs and output results.
    /// Sets Environment.ExitCode to 1 if any specs failed.
    /// </summary>
    /// <param name="json">If true, output JSON instead of console format</param>
    public static void run(bool json = false)
    {
        // Check for environment-based JSON output (CLI mode)
        var jsonOutputFile = Environment.GetEnvironmentVariable(JsonOutputFileEnvVar);
        if (!string.IsNullOrEmpty(jsonOutputFile))
        {
            // Add FileReporter for JSON output to the specified file
            var config = Configuration ?? new DraftSpecConfiguration();
            var reporter = new FileReporter(jsonOutputFile, new JsonFormatter(),
                allowedDirectory: Path.GetTempPath());
            config.AddReporter(reporter);
            Configuration = config;
        }

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
}

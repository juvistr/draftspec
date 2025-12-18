using DraftSpec.Configuration;
using DraftSpec.Formatters;
using DraftSpec.Providers;
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
    /// Environment variable name for tag filtering (comma-separated).
    /// Only specs with any of these tags will run.
    /// </summary>
    public const string FilterTagsEnvVar = "DRAFTSPEC_FILTER_TAGS";

    /// <summary>
    /// Environment variable name for tag exclusion (comma-separated).
    /// Specs with any of these tags will be skipped.
    /// </summary>
    public const string ExcludeTagsEnvVar = "DRAFTSPEC_EXCLUDE_TAGS";

    /// <summary>
    /// Environment variable name for name pattern filtering (regex).
    /// Only specs matching this pattern will run.
    /// </summary>
    public const string FilterNameEnvVar = "DRAFTSPEC_FILTER_NAME";

    /// <summary>
    /// Environment variable name for name pattern exclusion (regex).
    /// Specs matching this pattern will be skipped.
    /// </summary>
    public const string ExcludeNameEnvVar = "DRAFTSPEC_EXCLUDE_NAME";

    /// <summary>
    /// Run all collected specs and output results.
    /// Sets Environment.ExitCode to 1 if any specs failed.
    /// </summary>
    /// <param name="json">If true, output JSON instead of console format</param>
    public static void run(bool json = false)
    {
        var config = Configuration ?? new DraftSpecConfiguration();
        var env = config.EnvironmentProvider;

        // Check for environment-based JSON output (CLI mode)
        var jsonOutputFile = env.GetEnvironmentVariable(JsonOutputFileEnvVar);
        if (!string.IsNullOrEmpty(jsonOutputFile))
        {
            // Add FileReporter for JSON output to the specified file
            var reporter = new FileReporter(jsonOutputFile, new JsonFormatter(),
                Path.GetTempPath());
            config.AddReporter(reporter);
        }

        // Check for progress streaming (MCP mode)
        var progressStream = env.GetEnvironmentVariable(ProgressStreamEnvVar);
        if (progressStream is "true" or "1")
        {
            config.AddReporter(new ProgressStreamReporter());
        }

        Configuration = config;

        // Apply filter options from environment variables
        var builder = RunnerBuilder ?? new SpecRunnerBuilder();
        ApplyFilterEnvironmentVariables(builder, env);
        RunnerBuilder = builder;

        SpecExecutor.ExecuteAndOutput(RootContext, json, ResetState, RunnerBuilder, Configuration);
    }

    /// <summary>
    /// Apply filter options from environment variables to the runner builder.
    /// </summary>
    private static void ApplyFilterEnvironmentVariables(SpecRunnerBuilder builder, IEnvironmentProvider env)
    {
        // Tag filtering
        var filterTags = env.GetEnvironmentVariable(FilterTagsEnvVar);
        if (!string.IsNullOrEmpty(filterTags))
        {
            var tags = filterTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tags.Length > 0)
            {
                builder.WithTagFilter(tags);
            }
        }

        // Tag exclusion
        var excludeTags = env.GetEnvironmentVariable(ExcludeTagsEnvVar);
        if (!string.IsNullOrEmpty(excludeTags))
        {
            var tags = excludeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tags.Length > 0)
            {
                builder.WithoutTags(tags);
            }
        }

        // Name pattern filtering
        var filterName = env.GetEnvironmentVariable(FilterNameEnvVar);
        if (!string.IsNullOrEmpty(filterName))
        {
            builder.WithNameFilter(filterName);
        }

        // Name pattern exclusion - use inverted filter
        var excludeName = env.GetEnvironmentVariable(ExcludeNameEnvVar);
        if (!string.IsNullOrEmpty(excludeName))
        {
            builder.WithNameExcludeFilter(excludeName);
        }
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
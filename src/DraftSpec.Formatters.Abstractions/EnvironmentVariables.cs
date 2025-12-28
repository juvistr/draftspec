namespace DraftSpec.Formatters;

/// <summary>
/// Environment variable names used by DraftSpec for configuration.
/// </summary>
public static class EnvironmentVariables
{
    /// <summary>
    /// Environment variable name for CLI-based JSON output file path.
    /// When set, run() automatically adds a FileReporter to write JSON to this file.
    /// </summary>
    public const string JsonOutputFile = "DRAFTSPEC_JSON_OUTPUT_FILE";

    /// <summary>
    /// Environment variable name for enabling progress streaming.
    /// When set to "true" or "1", run() adds a ProgressStreamReporter for MCP integration.
    /// </summary>
    public const string ProgressStream = "DRAFTSPEC_PROGRESS_STREAM";

    /// <summary>
    /// Environment variable name for tag filtering (comma-separated).
    /// Only specs with any of these tags will run.
    /// </summary>
    public const string FilterTags = "DRAFTSPEC_FILTER_TAGS";

    /// <summary>
    /// Environment variable name for tag exclusion (comma-separated).
    /// Specs with any of these tags will be skipped.
    /// </summary>
    public const string ExcludeTags = "DRAFTSPEC_EXCLUDE_TAGS";

    /// <summary>
    /// Environment variable name for name pattern filtering (regex).
    /// Only specs matching this pattern will run.
    /// </summary>
    public const string FilterName = "DRAFTSPEC_FILTER_NAME";

    /// <summary>
    /// Environment variable name for name pattern exclusion (regex).
    /// Specs matching this pattern will be skipped.
    /// </summary>
    public const string ExcludeName = "DRAFTSPEC_EXCLUDE_NAME";
}

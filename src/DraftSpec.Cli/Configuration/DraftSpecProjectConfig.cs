using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Configuration;

/// <summary>
/// Project-level configuration loaded from draftspec.json.
/// All properties are nullable to distinguish between "not set" and "set to default value".
/// </summary>
public class DraftSpecProjectConfig
{
    /// <summary>
    /// Glob pattern to find spec files.
    /// Example: "**/*.spec.csx"
    /// </summary>
    [JsonPropertyName("specPattern")]
    public string? SpecPattern { get; set; }

    /// <summary>
    /// Default timeout in milliseconds for each spec.
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    /// <summary>
    /// Enable parallel execution of specs.
    /// </summary>
    [JsonPropertyName("parallel")]
    public bool? Parallel { get; set; }

    /// <summary>
    /// Maximum degree of parallelism when running specs in parallel.
    /// Defaults to processor count if not specified.
    /// </summary>
    [JsonPropertyName("maxParallelism")]
    public int? MaxParallelism { get; set; }

    /// <summary>
    /// List of reporters to use.
    /// Example: ["console", "json"]
    /// </summary>
    [JsonPropertyName("reporters")]
    public List<string>? Reporters { get; set; }

    /// <summary>
    /// Directory for output files (reports, etc.).
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Tag filtering configuration.
    /// </summary>
    [JsonPropertyName("tags")]
    public TagsConfig? Tags { get; set; }

    /// <summary>
    /// Stop execution after first spec failure.
    /// </summary>
    [JsonPropertyName("bail")]
    public bool? Bail { get; set; }

    /// <summary>
    /// Disable caching for dotnet-script compilation.
    /// </summary>
    [JsonPropertyName("noCache")]
    public bool? NoCache { get; set; }

    /// <summary>
    /// Output format for reports.
    /// Example: "console", "json", "html", "markdown"
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// Validates the configuration values.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Timeout.HasValue && Timeout.Value <= 0)
            errors.Add("timeout must be a positive number");

        if (MaxParallelism.HasValue && MaxParallelism.Value <= 0)
            errors.Add("maxParallelism must be a positive number");

        return errors;
    }
}

/// <summary>
/// Tag filtering configuration for including/excluding specs by tag.
/// </summary>
public class TagsConfig
{
    /// <summary>
    /// Tags to include. Only specs with any of these tags will run.
    /// </summary>
    [JsonPropertyName("include")]
    public List<string>? Include { get; set; }

    /// <summary>
    /// Tags to exclude. Specs with any of these tags will be skipped.
    /// </summary>
    [JsonPropertyName("exclude")]
    public List<string>? Exclude { get; set; }
}

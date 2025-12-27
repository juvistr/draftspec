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
    /// Code coverage configuration.
    /// </summary>
    [JsonPropertyName("coverage")]
    public CoverageConfig? Coverage { get; set; }

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

        // Validate coverage configuration
        if (Coverage?.Thresholds != null)
        {
            if (Coverage.Thresholds.Line is < 0 or > 100)
                errors.Add("coverage.thresholds.line must be between 0 and 100");
            if (Coverage.Thresholds.Branch is < 0 or > 100)
                errors.Add("coverage.thresholds.branch must be between 0 and 100");
        }

        if (Coverage?.Formats != null)
        {
            var validFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "cobertura", "xml", "html", "json", "coverage" };
            foreach (var format in Coverage.Formats.Where(f => !validFormats.Contains(f)))
                errors.Add($"Unknown coverage format: {format}");
        }

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

/// <summary>
/// Code coverage configuration.
/// </summary>
public class CoverageConfig
{
    /// <summary>
    /// Enable coverage collection by default.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Output directory for coverage reports.
    /// Default: ./coverage
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = "./coverage";

    /// <summary>
    /// Primary output format: cobertura, xml, coverage.
    /// Default: cobertura
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "cobertura";

    /// <summary>
    /// Additional output formats to generate.
    /// Example: ["cobertura", "html", "json"]
    /// </summary>
    [JsonPropertyName("formats")]
    public List<string>? Formats { get; set; }

    /// <summary>
    /// Additional report formats to generate from coverage data.
    /// Options: "html", "json"
    /// Example: ["html", "json"] generates HTML and JSON reports.
    /// </summary>
    [JsonPropertyName("reportFormats")]
    public List<string>? ReportFormats { get; set; }

    /// <summary>
    /// Glob patterns to exclude from coverage.
    /// Example: ["**/obj/**", "**/bin/**"]
    /// </summary>
    [JsonPropertyName("exclude")]
    public List<string>? Exclude { get; set; }

    /// <summary>
    /// Glob patterns to include in coverage.
    /// If specified, only matching files are included.
    /// </summary>
    [JsonPropertyName("include")]
    public List<string>? Include { get; set; }

    /// <summary>
    /// Coverage thresholds for pass/fail determination.
    /// </summary>
    [JsonPropertyName("thresholds")]
    public ThresholdsConfig? Thresholds { get; set; }
}

/// <summary>
/// Coverage threshold configuration.
/// </summary>
public class ThresholdsConfig
{
    /// <summary>
    /// Minimum line coverage percentage (0-100).
    /// </summary>
    [JsonPropertyName("line")]
    public double? Line { get; set; }

    /// <summary>
    /// Minimum branch coverage percentage (0-100).
    /// </summary>
    [JsonPropertyName("branch")]
    public double? Branch { get; set; }
}

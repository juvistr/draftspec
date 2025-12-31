using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Configuration;

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
using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Output format for spec execution results.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OutputFormat>))]
public enum OutputFormat
{
    /// <summary>
    /// Human-readable console output with colors and progress indicators.
    /// </summary>
    Console,

    /// <summary>
    /// JSON format for programmatic consumption.
    /// </summary>
    Json,

    /// <summary>
    /// Markdown format for documentation.
    /// </summary>
    Markdown,

    /// <summary>
    /// HTML format for web viewing.
    /// </summary>
    Html,

    /// <summary>
    /// JUnit XML format for CI/CD integration.
    /// </summary>
    JUnit
}

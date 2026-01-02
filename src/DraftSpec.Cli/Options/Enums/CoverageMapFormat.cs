using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Output format for the coverage-map command.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverageMapFormat>))]
public enum CoverageMapFormat
{
    /// <summary>
    /// Human-readable console output with color coding.
    /// </summary>
    Console,

    /// <summary>
    /// Machine-readable JSON for tooling integration.
    /// </summary>
    Json
}

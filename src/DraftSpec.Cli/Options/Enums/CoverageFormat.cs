using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Output format for code coverage reports.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverageFormat>))]
public enum CoverageFormat
{
    /// <summary>
    /// Cobertura XML format (default). Widely supported by CI/CD tools.
    /// </summary>
    Cobertura,

    /// <summary>
    /// Generic XML format.
    /// </summary>
    Xml,

    /// <summary>
    /// Native .coverage format (Visual Studio compatible).
    /// </summary>
    Coverage
}

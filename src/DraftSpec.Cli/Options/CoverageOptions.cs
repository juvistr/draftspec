using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Options;

/// <summary>
/// Composable options for code coverage collection.
/// Used by run command.
/// </summary>
public class CoverageOptions
{
    /// <summary>
    /// Enable code coverage collection via dotnet-coverage.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Output directory for coverage reports.
    /// Default: ./coverage
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Coverage output format: cobertura, xml, or coverage.
    /// Default: cobertura
    /// </summary>
    public CoverageFormat Format { get; set; } = CoverageFormat.Cobertura;

    /// <summary>
    /// Additional coverage report formats to generate (comma-separated).
    /// Options: html, json
    /// Example: "html,json" generates both HTML and JSON reports.
    /// </summary>
    public string? ReportFormats { get; set; }

    /// <summary>
    /// Returns the output directory, defaulting to "./coverage" if not set.
    /// </summary>
    public string OutputDirectory => Output ?? "./coverage";
}

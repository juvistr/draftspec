using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'coverage-map' command.
/// Maps specs to source code methods.
/// </summary>
public class CoverageMapOptions
{
    /// <summary>
    /// Path to source files or directory to analyze.
    /// </summary>
    public string SourcePath { get; set; } = ".";

    /// <summary>
    /// Path to spec files or directory.
    /// If not specified, searches for *.spec.csx in the project.
    /// </summary>
    public string? SpecPath { get; set; }

    /// <summary>
    /// Output format: console or json.
    /// </summary>
    public CoverageMapFormat Format { get; set; } = CoverageMapFormat.Console;

    /// <summary>
    /// Show only uncovered methods (gaps).
    /// </summary>
    public bool GapsOnly { get; set; }

    /// <summary>
    /// Filter to specific namespaces (comma-separated).
    /// </summary>
    public string? NamespaceFilter { get; set; }
}

using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'docs' command.
/// Generates living documentation from spec structure.
/// </summary>
public class DocsOptions
{
    /// <summary>
    /// Path to spec files or directory.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Output format for documentation: markdown or html.
    /// </summary>
    public DocsFormat Format { get; set; } = DocsFormat.Markdown;

    /// <summary>
    /// Filter to a specific describe/context block.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Include test results from a previous run.
    /// </summary>
    public bool WithResults { get; set; }

    /// <summary>
    /// Path to JSON results file (used with --with-results).
    /// </summary>
    public string? ResultsFile { get; set; }

    /// <summary>
    /// Filter options for selecting which specs to include.
    /// </summary>
    public FilterOptions Filter { get; set; } = new();
}

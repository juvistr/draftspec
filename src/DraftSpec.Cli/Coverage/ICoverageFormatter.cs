namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Interface for formatting coverage reports into different output formats.
/// </summary>
public interface ICoverageFormatter
{
    /// <summary>
    /// Format a coverage report into a string representation.
    /// </summary>
    /// <param name="report">The coverage report to format.</param>
    /// <returns>Formatted string representation of the coverage report.</returns>
    string Format(CoverageReport report);

    /// <summary>
    /// The file extension typically used for this format (e.g., ".html", ".xml").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// The format name for CLI identification (e.g., "html", "cobertura").
    /// </summary>
    string FormatName { get; }
}

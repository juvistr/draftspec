namespace DraftSpec.Formatters;

/// <summary>
/// Interface for formatting spec reports into different output formats.
/// </summary>
public interface IFormatter
{
    /// <summary>
    /// Format a spec report into a string representation.
    /// </summary>
    string Format(SpecReport report);

    /// <summary>
    /// The file extension typically used for this format (e.g., ".md", ".html").
    /// </summary>
    string FileExtension { get; }
}
namespace DraftSpec.Formatters;

/// <summary>
/// Interface for formatting spec reports into different output formats.
/// </summary>
/// <remarks>
/// This is the base interface for all formatters. Implementations can override
/// <see cref="Format(SpecReport, TextWriter)"/> for stream-based output, or just
/// implement <see cref="Format(SpecReport)"/> and use the default stream implementation.
/// </remarks>
public interface IFormatter
{
    /// <summary>
    /// Format a spec report into a string representation.
    /// </summary>
    string Format(SpecReport report);

    /// <summary>
    /// Format a spec report and write to the provided TextWriter.
    /// </summary>
    /// <remarks>
    /// Default implementation calls <see cref="Format(SpecReport)"/> and writes the result.
    /// Override for streaming output or when the formatter needs direct TextWriter access.
    /// </remarks>
    void Format(SpecReport report, TextWriter output)
    {
        output.Write(Format(report));
    }

    /// <summary>
    /// The file extension typically used for this format (e.g., ".md", ".html").
    /// </summary>
    string FileExtension { get; }
}

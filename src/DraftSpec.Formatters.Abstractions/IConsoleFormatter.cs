namespace DraftSpec.Formatters;

/// <summary>
/// Interface for formatters that write directly to console (supporting colors/styling).
/// Unlike IFormatter which returns a string, IConsoleFormatter writes to a TextWriter
/// to support ANSI color codes and interactive terminal features.
/// </summary>
public interface IConsoleFormatter
{
    /// <summary>
    /// Format and write a spec report to the provided TextWriter.
    /// </summary>
    /// <param name="report">The spec report to format</param>
    /// <param name="output">The TextWriter to write to (e.g., Console.Out)</param>
    /// <param name="useColors">Whether to include ANSI color codes</param>
    void Format(SpecReport report, TextWriter output, bool useColors = true);
}
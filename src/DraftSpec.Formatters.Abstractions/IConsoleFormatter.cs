namespace DraftSpec.Formatters;

/// <summary>
/// Interface for formatters that write directly to console (supporting colors/styling).
/// Extends <see cref="IFormatter"/> with color support for interactive terminal output.
/// </summary>
/// <remarks>
/// Use this interface when your formatter needs to output ANSI color codes or other
/// terminal-specific features. The <see cref="Format(SpecReport, TextWriter, bool)"/> method
/// provides control over whether colors are enabled.
/// </remarks>
public interface IConsoleFormatter : IFormatter
{
    /// <summary>
    /// Format and write a spec report to the provided TextWriter with color control.
    /// </summary>
    /// <param name="report">The spec report to format</param>
    /// <param name="output">The TextWriter to write to (e.g., Console.Out)</param>
    /// <param name="useColors">Whether to include ANSI color codes</param>
    void Format(SpecReport report, TextWriter output, bool useColors);
}

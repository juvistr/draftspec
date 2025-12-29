namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Factory for creating coverage formatters.
/// </summary>
public interface ICoverageFormatterFactory
{
    /// <summary>
    /// Get a formatter by name.
    /// </summary>
    /// <param name="format">The format name (e.g., "html", "json").</param>
    /// <returns>The formatter, or null if format is not supported.</returns>
    ICoverageFormatter? GetFormatter(string format);
}

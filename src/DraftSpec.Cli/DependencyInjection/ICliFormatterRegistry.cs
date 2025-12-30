using DraftSpec.Formatters;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Registry for CLI formatters with factory-based lookup.
/// </summary>
public interface ICliFormatterRegistry
{
    /// <summary>
    /// Gets a formatter by name.
    /// </summary>
    /// <param name="name">The formatter name (e.g., "json", "html", "markdown").</param>
    /// <param name="cssUrl">Optional CSS URL for HTML output.</param>
    /// <returns>The configured formatter, or null if not found.</returns>
    IFormatter? GetFormatter(string name, string? cssUrl = null);

    /// <summary>
    /// Registers a formatter factory with the given name.
    /// </summary>
    /// <param name="name">The formatter name.</param>
    /// <param name="factory">Factory function that creates the formatter.</param>
    void Register(string name, Func<string?, IFormatter> factory);

    /// <summary>
    /// Gets all registered formatter names.
    /// </summary>
    IEnumerable<string> Names { get; }
}

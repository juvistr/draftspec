using DraftSpec.Formatters;

namespace DraftSpec.Plugins;

/// <summary>
/// Registry for spec report formatters.
/// </summary>
public interface IFormatterRegistry
{
    /// <summary>
    /// Register a formatter with a name.
    /// </summary>
    /// <param name="name">The formatter name (e.g., "json", "html")</param>
    /// <param name="formatter">The formatter instance</param>
    void Register(string name, IFormatter formatter);

    /// <summary>
    /// Get a formatter by name.
    /// </summary>
    /// <param name="name">The formatter name</param>
    /// <returns>The formatter, or null if not found</returns>
    IFormatter? GetByName(string name);

    /// <summary>
    /// Get all registered formatter names.
    /// </summary>
    IEnumerable<string> Names { get; }

    /// <summary>
    /// Check if a formatter is registered.
    /// </summary>
    /// <param name="name">The formatter name</param>
    bool Contains(string name);
}
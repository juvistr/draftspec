using DraftSpec.Formatters;
using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Registry for output formatters. Stores formatters by name for lookup.
/// </summary>
/// <remarks>
/// Names are case-insensitive. Built-in formatters (json, markdown, html)
/// are registered automatically. Custom formatters can be added via plugins
/// or directly through <see cref="DraftSpecConfiguration.AddFormatter"/>.
/// </remarks>
public class FormatterRegistry : IFormatterRegistry
{
    private readonly Dictionary<string, IFormatter> _formatters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a formatter with the specified name.
    /// </summary>
    /// <param name="name">The name to register the formatter under (case-insensitive).</param>
    /// <param name="formatter">The formatter instance.</param>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when formatter is null.</exception>
    public void Register(string name, IFormatter formatter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(formatter);
        _formatters[name] = formatter;
    }

    /// <summary>
    /// Gets a formatter by name.
    /// </summary>
    /// <param name="name">The formatter name (case-insensitive).</param>
    /// <returns>The formatter, or null if not found.</returns>
    public IFormatter? Get(string name)
    {
        return _formatters.TryGetValue(name, out var formatter) ? formatter : null;
    }

    /// <summary>
    /// Gets all registered formatter names.
    /// </summary>
    public IEnumerable<string> Names => _formatters.Keys;

    /// <summary>
    /// Checks if a formatter with the specified name is registered.
    /// </summary>
    /// <param name="name">The formatter name (case-insensitive).</param>
    /// <returns>True if a formatter with the name is registered.</returns>
    public bool Contains(string name)
    {
        return _formatters.ContainsKey(name);
    }
}
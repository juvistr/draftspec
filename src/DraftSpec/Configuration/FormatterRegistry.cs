using DraftSpec.Formatters;
using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Implementation of the formatter registry.
/// </summary>
public class FormatterRegistry : IFormatterRegistry
{
    private readonly Dictionary<string, IFormatter> _formatters = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, IFormatter formatter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(formatter);
        _formatters[name] = formatter;
    }

    public IFormatter? Get(string name)
    {
        return _formatters.TryGetValue(name, out var formatter) ? formatter : null;
    }

    public IEnumerable<string> Names => _formatters.Keys;

    public bool Contains(string name) => _formatters.ContainsKey(name);
}

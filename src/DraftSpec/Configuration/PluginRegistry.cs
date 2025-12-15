using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Internal registry for managing plugin instances.
/// </summary>
internal class PluginRegistry : IDisposable
{
    private readonly List<IPlugin> _plugins = [];
    private readonly Dictionary<Type, IPlugin> _byType = [];
    private bool _disposed;

    public void Register(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        _plugins.Add(plugin);
        _byType[plugin.GetType()] = plugin;
    }

    public T? Get<T>() where T : class, IPlugin
    {
        return _byType.TryGetValue(typeof(T), out var plugin) ? plugin as T : null;
    }

    public IEnumerable<IPlugin> All => _plugins;

    public IEnumerable<T> OfType<T>() where T : IPlugin
    {
        return _plugins.OfType<T>();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var plugin in _plugins)
        {
            plugin.Dispose();
        }
        _plugins.Clear();
        _byType.Clear();
    }
}

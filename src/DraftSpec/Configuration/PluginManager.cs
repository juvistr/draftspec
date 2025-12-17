using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Manages plugin registration, discovery, and lifecycle.
/// </summary>
public class PluginManager : IDisposable
{
    private readonly List<IPlugin> _plugins = [];
    private readonly Dictionary<Type, IPlugin> _byType = [];
    private bool _disposed;

    /// <summary>
    /// Register a plugin instance.
    /// </summary>
    /// <param name="plugin">The plugin to register</param>
    public void Register(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        _plugins.Add(plugin);
        _byType[plugin.GetType()] = plugin;
    }

    /// <summary>
    /// Register a plugin by type.
    /// </summary>
    /// <typeparam name="T">The plugin type</typeparam>
    public void Register<T>() where T : IPlugin, new()
    {
        Register(new T());
    }

    /// <summary>
    /// Get a plugin by its type.
    /// </summary>
    /// <typeparam name="T">The plugin type</typeparam>
    /// <returns>The plugin instance, or null if not registered</returns>
    public T? Get<T>() where T : class, IPlugin
    {
        return _byType.TryGetValue(typeof(T), out var plugin) ? plugin as T : null;
    }

    /// <summary>
    /// Get all registered plugins.
    /// </summary>
    public IEnumerable<IPlugin> All => _plugins;

    /// <summary>
    /// Get all plugins of a specific type.
    /// </summary>
    /// <typeparam name="T">The plugin type to filter by</typeparam>
    public IEnumerable<T> OfType<T>() where T : IPlugin
    {
        return _plugins.OfType<T>();
    }

    /// <summary>
    /// Get the count of registered plugins.
    /// </summary>
    public int Count => _plugins.Count;

    /// <summary>
    /// Dispose all registered plugins.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var plugin in _plugins) plugin.Dispose();
        _plugins.Clear();
        _byType.Clear();
    }
}

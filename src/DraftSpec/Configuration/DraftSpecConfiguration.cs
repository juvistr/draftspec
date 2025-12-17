using DraftSpec.Formatters;
using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Main configuration class for DraftSpec.
/// Manages plugins, formatters, reporters, and services.
/// </summary>
public class DraftSpecConfiguration : IDraftSpecConfiguration, IDisposable
{
    private readonly PluginRegistry _pluginRegistry = new();
    private readonly FormatterRegistry _formatterRegistry = new();
    private readonly ReporterRegistry _reporterRegistry = new();
    private readonly Dictionary<Type, object> _services = [];
    private readonly PluginContext _pluginContext;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Create a new DraftSpec configuration instance.
    /// </summary>
    public DraftSpecConfiguration()
    {
        _pluginContext = new PluginContext(this);
    }

    /// <summary>
    /// Console formatter for outputting results to the terminal.
    /// Set this to enable colored console output during spec execution.
    /// </summary>
    public IConsoleFormatter? ConsoleFormatter { get; set; }

    /// <inheritdoc />
    public IFormatterRegistry Formatters => _formatterRegistry;

    /// <inheritdoc />
    public IReporterRegistry Reporters => _reporterRegistry;

    /// <inheritdoc />
    public IEnumerable<IPlugin> Plugins => _pluginRegistry.All;

    /// <summary>
    /// Register a plugin by type.
    /// </summary>
    /// <typeparam name="T">The plugin type</typeparam>
    public DraftSpecConfiguration UsePlugin<T>() where T : IPlugin, new()
    {
        return UsePlugin(new T());
    }

    /// <summary>
    /// Register a plugin instance.
    /// </summary>
    /// <param name="plugin">The plugin to register</param>
    public DraftSpecConfiguration UsePlugin(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        _pluginRegistry.Register(plugin);
        return this;
    }

    /// <summary>
    /// Configure a registered plugin.
    /// </summary>
    /// <typeparam name="T">The plugin type</typeparam>
    /// <param name="configure">Configuration action</param>
    public DraftSpecConfiguration Configure<T>(Action<T> configure) where T : class, IPlugin
    {
        var plugin = _pluginRegistry.Get<T>();
        if (plugin != null)
        {
            configure(plugin);
        }
        return this;
    }

    /// <summary>
    /// Register a reporter directly (without a plugin wrapper).
    /// </summary>
    /// <param name="reporter">The reporter to register</param>
    public DraftSpecConfiguration AddReporter(IReporter reporter)
    {
        _reporterRegistry.Register(reporter);
        return this;
    }

    /// <summary>
    /// Register a formatter directly (without a plugin wrapper).
    /// </summary>
    /// <param name="name">The formatter name</param>
    /// <param name="formatter">The formatter to register</param>
    public DraftSpecConfiguration AddFormatter(string name, IFormatter formatter)
    {
        _formatterRegistry.Register(name, formatter);
        return this;
    }

    /// <summary>
    /// Register a service for dependency injection.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <param name="service">The service instance</param>
    public DraftSpecConfiguration AddService<T>(T service) where T : class
    {
        ArgumentNullException.ThrowIfNull(service);
        _services[typeof(T)] = service;
        return this;
    }

    /// <inheritdoc />
    public T? GetService<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
    }

    /// <summary>
    /// Initialize all registered plugins.
    /// Called automatically when the configuration is used.
    /// </summary>
    internal void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Initialize all plugins
        foreach (var plugin in _pluginRegistry.All)
        {
            plugin.Initialize(_pluginContext);
        }

        // Let formatter plugins register their formatters
        foreach (var plugin in _pluginRegistry.OfType<IFormatterPlugin>())
        {
            plugin.RegisterFormatters(_formatterRegistry);
        }

        // Let reporter plugins register their reporters
        foreach (var plugin in _pluginRegistry.OfType<IReporterPlugin>())
        {
            plugin.RegisterReporters(_reporterRegistry);
        }
    }

    /// <summary>
    /// Initialize middleware plugins with a builder.
    /// </summary>
    /// <param name="builder">The spec runner builder</param>
    internal void InitializeMiddleware(SpecRunnerBuilder builder)
    {
        foreach (var plugin in _pluginRegistry.OfType<IMiddlewarePlugin>())
        {
            plugin.RegisterMiddleware(builder);
        }
    }

    /// <summary>
    /// Dispose all registered plugins and release resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pluginRegistry.Dispose();
    }
}

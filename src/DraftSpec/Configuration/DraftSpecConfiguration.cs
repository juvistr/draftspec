using DraftSpec.Formatters;
using DraftSpec.Providers;
using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Main configuration class for DraftSpec.
/// Acts as a facade coordinating plugins, formatters, reporters, and services.
/// </summary>
public class DraftSpecConfiguration : IDraftSpecConfiguration, IDisposable
{
    private readonly PluginContext _pluginContext;
    private int _initialized;
    private int _disposed;

    /// <summary>
    /// Create a new DraftSpec configuration instance.
    /// </summary>
    public DraftSpecConfiguration()
    {
        Services = new ServiceRegistry();
        Plugins = new PluginManager();
        Formatters = new FormatterRegistry();
        Reporters = new ReporterRegistry();
        _pluginContext = new PluginContext(this);
    }

    /// <summary>
    /// Service registry for dependency injection.
    /// </summary>
    public ServiceRegistry Services { get; }

    /// <summary>
    /// Plugin manager for plugin lifecycle.
    /// </summary>
    public PluginManager Plugins { get; }

    /// <summary>
    /// Console formatter for outputting results to the terminal.
    /// Set this to enable colored console output during spec execution.
    /// </summary>
    public IConsoleFormatter? ConsoleFormatter { get; set; }

    /// <summary>
    /// Environment provider for accessing environment variables.
    /// Defaults to <see cref="SystemEnvironmentProvider.Instance"/>.
    /// Set to <see cref="InMemoryEnvironmentProvider"/> for testing.
    /// </summary>
    public IEnvironmentProvider EnvironmentProvider { get; set; } = SystemEnvironmentProvider.Instance;

    /// <inheritdoc />
    public IFormatterRegistry Formatters { get; }

    /// <inheritdoc />
    public IReporterRegistry Reporters { get; }

    /// <inheritdoc />
    IEnumerable<IPlugin> IDraftSpecConfiguration.Plugins => Plugins.All;

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
        Plugins.Register(plugin);
        return this;
    }

    /// <summary>
    /// Configure a registered plugin.
    /// </summary>
    /// <typeparam name="T">The plugin type</typeparam>
    /// <param name="configure">Configuration action</param>
    public DraftSpecConfiguration Configure<T>(Action<T> configure) where T : class, IPlugin
    {
        var plugin = Plugins.Get<T>();
        if (plugin != null) configure(plugin);
        return this;
    }

    /// <summary>
    /// Register a reporter directly (without a plugin wrapper).
    /// </summary>
    /// <param name="reporter">The reporter to register</param>
    public DraftSpecConfiguration AddReporter(IReporter reporter)
    {
        ((ReporterRegistry)Reporters).Register(reporter);
        return this;
    }

    /// <summary>
    /// Register a formatter directly (without a plugin wrapper).
    /// </summary>
    /// <param name="name">The formatter name</param>
    /// <param name="formatter">The formatter to register</param>
    public DraftSpecConfiguration AddFormatter(string name, IFormatter formatter)
    {
        ((FormatterRegistry)Formatters).Register(name, formatter);
        return this;
    }

    /// <summary>
    /// Register a service for dependency injection.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <param name="service">The service instance</param>
    public DraftSpecConfiguration AddService<T>(T service) where T : class
    {
        Services.Register(service);
        return this;
    }

    /// <inheritdoc />
    public T? GetService<T>() where T : class
    {
        return Services.GetService<T>();
    }

    /// <summary>
    /// Initialize all registered plugins.
    /// Called automatically when the configuration is used.
    /// </summary>
    internal void Initialize()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        // Initialize all plugins
        foreach (var plugin in Plugins.All) plugin.Initialize(_pluginContext);

        // Let formatter plugins register their formatters
        foreach (var plugin in Plugins.OfType<IFormatterPlugin>())
            plugin.RegisterFormatters(Formatters);

        // Let reporter plugins register their reporters
        foreach (var plugin in Plugins.OfType<IReporterPlugin>())
            plugin.RegisterReporters(Reporters);
    }

    /// <summary>
    /// Initialize middleware plugins with a builder.
    /// </summary>
    /// <param name="builder">The spec runner builder</param>
    internal void InitializeMiddleware(SpecRunnerBuilder builder)
    {
        foreach (var plugin in Plugins.OfType<IMiddlewarePlugin>())
            plugin.RegisterMiddleware(builder);
    }

    /// <summary>
    /// Dispose all registered plugins and release resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;
        Plugins.Dispose();
        GC.SuppressFinalize(this);
    }
}

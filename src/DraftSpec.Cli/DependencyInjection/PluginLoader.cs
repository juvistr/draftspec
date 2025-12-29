using System.Reflection;
using System.Runtime.Loader;
using DraftSpec.Formatters;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Interface for loading plugins from assemblies.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Discovers and loads all plugins from configured directories.
    /// </summary>
    IEnumerable<PluginInfo> DiscoverPlugins();

    /// <summary>
    /// Registers discovered formatters with the CLI registry.
    /// </summary>
    void RegisterFormatters(ICliFormatterRegistry registry);
}

/// <summary>
/// Information about a discovered plugin.
/// </summary>
public record PluginInfo(
    string Name,
    Type Type,
    PluginKind Kind,
    Assembly Assembly);

/// <summary>
/// Kind of plugin discovered.
/// </summary>
public enum PluginKind
{
    Formatter,
    Reporter
}

/// <summary>
/// Attribute to mark a class as a DraftSpec plugin for auto-discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class DraftSpecPluginAttribute : Attribute
{
    /// <summary>
    /// The name used to reference this plugin (e.g., "json", "html").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional description of the plugin.
    /// </summary>
    public string? Description { get; set; }

    public DraftSpecPluginAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Loads plugins from assemblies in specified directories.
/// </summary>
public class PluginLoader : IPluginLoader
{
    private readonly string[] _pluginDirectories;
    private readonly IPluginScanner _scanner;
    private readonly IAssemblyLoader _assemblyLoader;
    private readonly IConsole _console;
    private readonly List<PluginInfo> _discoveredPlugins = [];

    /// <summary>
    /// Creates a new PluginLoader with default implementations.
    /// </summary>
    public PluginLoader(params string[] pluginDirectories)
        : this(new SystemPluginScanner(), new IsolatedAssemblyLoader(), new SystemConsole(), pluginDirectories)
    {
    }

    /// <summary>
    /// Creates a new PluginLoader with custom implementations for testability.
    /// </summary>
    public PluginLoader(
        IPluginScanner scanner,
        IAssemblyLoader assemblyLoader,
        IConsole console,
        params string[] pluginDirectories)
    {
        _scanner = scanner;
        _assemblyLoader = assemblyLoader;
        _console = console;
        _pluginDirectories = pluginDirectories.Length > 0
            ? pluginDirectories
            : [GetDefaultPluginDirectory()];
    }

    private static string GetDefaultPluginDirectory()
    {
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Combine(exeDir ?? ".", "plugins");
    }

    public IEnumerable<PluginInfo> DiscoverPlugins()
    {
        if (_discoveredPlugins.Count > 0)
            return _discoveredPlugins;

        foreach (var directory in _pluginDirectories)
        {
            if (!_scanner.DirectoryExists(directory))
                continue;

            foreach (var dllPath in _scanner.FindPluginFiles(directory))
                try
                {
                    var plugins = LoadPluginsFromAssembly(dllPath);
                    _discoveredPlugins.AddRange(plugins);
                }
                catch (Exception ex)
                {
                    // Log but don't fail - plugin loading should be resilient
                    _console.WriteError($"Warning: Failed to load plugin from {dllPath}: {ex.Message}");
                }
        }

        return _discoveredPlugins;
    }

    private IEnumerable<PluginInfo> LoadPluginsFromAssembly(string assemblyPath)
    {
        var assembly = _assemblyLoader.LoadAssembly(assemblyPath);
        if (assembly == null)
            yield break;

        foreach (var type in _assemblyLoader.GetExportedTypes(assembly))
        {
            var attribute = type.GetCustomAttribute<DraftSpecPluginAttribute>();
            if (attribute == null)
                continue;

            PluginKind kind;
            if (typeof(IFormatter).IsAssignableFrom(type))
                kind = PluginKind.Formatter;
            else
                continue; // Skip types that don't implement known plugin interfaces

            yield return new PluginInfo(attribute.Name, type, kind, assembly);
        }
    }

    public void RegisterFormatters(ICliFormatterRegistry registry)
    {
        foreach (var plugin in DiscoverPlugins().Where(p => p.Kind == PluginKind.Formatter))
            registry.Register(plugin.Name, _ =>
            {
                var instance = _assemblyLoader.CreateInstance(plugin.Type);
                return (IFormatter)instance!;
            });
    }
}

/// <summary>
/// Custom AssemblyLoadContext for plugin isolation.
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve from plugin directory first
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        // Fall back to default context for shared framework assemblies
        return null;
    }
}

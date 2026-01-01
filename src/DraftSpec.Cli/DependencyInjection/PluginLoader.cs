using System.Reflection;
using DraftSpec.Cli.Configuration;
using DraftSpec.Formatters;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Loads plugins from assemblies in specified directories.
/// </summary>
public class PluginLoader : IPluginLoader
{
    private readonly IPluginScanner _scanner;
    private readonly IAssemblyLoader _assemblyLoader;
    private readonly IConsole _console;
    private readonly PluginsConfig? _pluginsConfig;
    private readonly List<PluginInfo> _discoveredPlugins = [];

    /// <summary>
    /// Creates a new PluginLoader with injected dependencies.
    /// </summary>
    /// <param name="scanner">Scanner for finding plugin files.</param>
    /// <param name="assemblyLoader">Loader for loading plugin assemblies.</param>
    /// <param name="console">Console for logging.</param>
    /// <param name="pluginsConfig">Optional plugin security configuration.</param>
    public PluginLoader(
        IPluginScanner scanner,
        IAssemblyLoader assemblyLoader,
        IConsole console,
        PluginsConfig? pluginsConfig = null)
    {
        _scanner = scanner;
        _assemblyLoader = assemblyLoader;
        _console = console;
        _pluginsConfig = pluginsConfig;
    }

    private static string GetDefaultPluginDirectory()
    {
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Combine(exeDir ?? ".", "plugins");
    }

    public IEnumerable<PluginInfo> DiscoverPlugins(string[]? directories = null)
    {
        if (_discoveredPlugins.Count > 0)
            return _discoveredPlugins;

        var pluginDirs = directories is { Length: > 0 }
            ? directories
            : [GetDefaultPluginDirectory()];

        foreach (var directory in pluginDirs)
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
        // Verify signature if required
        if (_pluginsConfig?.RequireSignedPlugins == true)
        {
            var publicKeyToken = _assemblyLoader.GetPublicKeyToken(assemblyPath);
            if (publicKeyToken == null)
            {
                _console.WriteError($"Warning: Plugin {Path.GetFileName(assemblyPath)} is not signed. Skipping.");
                yield break;
            }

            if (_pluginsConfig.TrustedPublicKeyTokens?.Contains(publicKeyToken, StringComparer.OrdinalIgnoreCase) != true)
            {
                _console.WriteError($"Warning: Plugin {Path.GetFileName(assemblyPath)} has untrusted signature ({publicKeyToken}). Skipping.");
                yield break;
            }
        }

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

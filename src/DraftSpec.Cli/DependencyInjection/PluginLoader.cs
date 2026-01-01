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
            if (!IsPluginTrusted(assemblyPath))
                yield break;
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

    /// <summary>
    /// Checks if a plugin is trusted based on public key token or certificate thumbprint.
    /// </summary>
    /// <param name="assemblyPath">Path to the plugin assembly.</param>
    /// <returns>True if the plugin is trusted, false otherwise.</returns>
    private bool IsPluginTrusted(string assemblyPath)
    {
        var fileName = Path.GetFileName(assemblyPath);

        // Check certificate thumbprint first (stronger verification)
        var certThumbprint = _assemblyLoader.GetCertificateThumbprint(assemblyPath);
        if (certThumbprint != null)
        {
            if (_pluginsConfig?.TrustedCertificateThumbprints?.Contains(
                    certThumbprint, StringComparer.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        // Fall back to public key token check
        var publicKeyToken = _assemblyLoader.GetPublicKeyToken(assemblyPath);
        if (publicKeyToken != null)
        {
            if (_pluginsConfig?.TrustedPublicKeyTokens?.Contains(
                    publicKeyToken, StringComparer.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        // Not signed at all
        if (publicKeyToken == null && certThumbprint == null)
        {
            _console.WriteError($"Warning: Plugin {fileName} is not signed. Skipping.");
            return false;
        }

        // Signed but not trusted
        var signatureInfo = certThumbprint != null
            ? $"certificate: {certThumbprint[..16]}..."
            : $"public key token: {publicKeyToken}";
        _console.WriteError($"Warning: Plugin {fileName} has untrusted signature ({signatureInfo}). Skipping.");
        return false;
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

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Interface for loading plugins from assemblies.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Discovers and loads all plugins from the specified directories.
    /// </summary>
    /// <param name="directories">Optional directories to search. If null, uses default plugin directory.</param>
    IEnumerable<PluginInfo> DiscoverPlugins(string[]? directories = null);

    /// <summary>
    /// Registers discovered formatters with the CLI registry.
    /// </summary>
    void RegisterFormatters(ICliFormatterRegistry registry);
}

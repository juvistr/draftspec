namespace DraftSpec.Plugins;

/// <summary>
/// Base interface for all DraftSpec plugins.
/// Plugins provide extensibility points for formatters, reporters, and middleware.
/// </summary>
public interface IPlugin : IDisposable
{
    /// <summary>
    /// The unique name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The version of the plugin (e.g., "1.0.0").
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Initialize the plugin with the given context.
    /// Called once when the plugin is registered.
    /// </summary>
    /// <param name="context">Plugin context providing access to services and configuration</param>
    void Initialize(IPluginContext context);
}
namespace DraftSpec.Plugins;

/// <summary>
/// Plugin that registers custom formatters.
/// Formatters transform spec reports into specific output formats.
/// </summary>
public interface IFormatterPlugin : IPlugin
{
    /// <summary>
    /// Register formatters with the registry.
    /// Called after Initialize().
    /// </summary>
    /// <param name="registry">The formatter registry</param>
    void RegisterFormatters(IFormatterRegistry registry);
}
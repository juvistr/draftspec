namespace DraftSpec.Plugins;

/// <summary>
/// Plugin that registers custom reporters.
/// Reporters perform side effects during spec execution (file writes, notifications, etc.).
/// </summary>
public interface IReporterPlugin : IPlugin
{
    /// <summary>
    /// Register reporters with the registry.
    /// Called after Initialize().
    /// </summary>
    /// <param name="registry">The reporter registry</param>
    void RegisterReporters(IReporterRegistry registry);
}

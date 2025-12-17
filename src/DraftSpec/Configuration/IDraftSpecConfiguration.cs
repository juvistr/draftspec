using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Interface for DraftSpec configuration.
/// </summary>
public interface IDraftSpecConfiguration
{
    /// <summary>
    /// Get the formatter registry.
    /// </summary>
    IFormatterRegistry Formatters { get; }

    /// <summary>
    /// Get the reporter registry.
    /// </summary>
    IReporterRegistry Reporters { get; }

    /// <summary>
    /// Get all registered plugins.
    /// </summary>
    IEnumerable<IPlugin> Plugins { get; }

    /// <summary>
    /// Get a service by type.
    /// </summary>
    T? GetService<T>() where T : class;
}
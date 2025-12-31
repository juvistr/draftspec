namespace DraftSpec.Plugins;

/// <summary>
/// Context provided to plugins during initialization.
/// Gives access to services, configuration, and logging.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Get a registered service by type.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance, or null if not registered</returns>
    T? GetService<T>() where T : class;

    /// <summary>
    /// Get a registered service by type (throws if not found).
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not registered</exception>
    T GetRequiredService<T>() where T : class;

    /// <summary>
    /// Log a message from the plugin.
    /// </summary>
    /// <param name="level">Log level</param>
    /// <param name="message">Log message</param>
    void Log(LogLevel level, string message);
}

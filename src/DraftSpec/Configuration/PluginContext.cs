using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Implementation of IPluginContext provided to plugins during initialization.
/// </summary>
internal class PluginContext : IPluginContext
{
    private readonly DraftSpecConfiguration _configuration;

    public PluginContext(DraftSpecConfiguration configuration)
    {
        _configuration = configuration;
    }

    public T? GetService<T>() where T : class
    {
        return _configuration.GetService<T>();
    }

    public T GetRequiredService<T>() where T : class
    {
        return GetService<T>()
               ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered");
    }

    public void Log(LogLevel level, string message)
    {
        // Simple console logging for now
        // Could be extended to use a registered ILogger service
        var prefix = level switch
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERROR]",
            _ => "[???]"
        };
        Console.WriteLine($"{prefix} {message}");
    }
}
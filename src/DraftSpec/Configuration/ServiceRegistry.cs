namespace DraftSpec.Configuration;

/// <summary>
/// Simple service registry for dependency injection.
/// Provides service registration and resolution without external DI container dependencies.
/// </summary>
public class ServiceRegistry
{
    private readonly Dictionary<Type, object> _services = [];

    /// <summary>
    /// Register a service instance.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <param name="service">The service instance</param>
    public void Register<T>(T service) where T : class
    {
        ArgumentNullException.ThrowIfNull(service);
        _services[typeof(T)] = service;
    }

    /// <summary>
    /// Get a service by type.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance, or null if not registered</returns>
    public T? GetService<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
    }

    /// <summary>
    /// Check if a service is registered.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>True if the service is registered</returns>
    public bool HasService<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Get the count of registered services.
    /// </summary>
    public int Count => _services.Count;
}

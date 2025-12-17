namespace DraftSpec.Plugins;

/// <summary>
/// Plugin that registers custom middleware.
/// Middleware wraps spec execution to add cross-cutting concerns.
/// </summary>
public interface IMiddlewarePlugin : IPlugin
{
    /// <summary>
    /// Register middleware with the runner builder.
    /// Called after Initialize().
    /// </summary>
    /// <param name="builder">The spec runner builder</param>
    void RegisterMiddleware(SpecRunnerBuilder builder);
}
using DraftSpec.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Mcp.DependencyInjection;

/// <summary>
/// Extension methods for registering DraftSpec MCP services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DraftSpec MCP services to the service collection.
    /// </summary>
    public static IServiceCollection AddDraftSpecMcp(this IServiceCollection services)
    {
        services.AddLogging();

        services.AddSingleton<TempFileManager>();
        services.AddSingleton<IAsyncProcessRunner, SystemAsyncProcessRunner>();
        services.AddSingleton<SessionManager>();
        services.AddSingleton<SpecExecutionService>();
        services.AddSingleton<ISpecExecutionService>(sp => sp.GetRequiredService<SpecExecutionService>());

        return services;
    }
}

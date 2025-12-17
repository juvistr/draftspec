using DraftSpec.Formatters;
using DraftSpec.Formatters.Html;
using DraftSpec.Formatters.Markdown;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Extension methods for registering DraftSpec services with DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all DraftSpec CLI services including formatters, finders, and runners.
    /// </summary>
    public static IServiceCollection AddDraftSpec(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<ISpecFinder, SpecFinder>();
        services.AddSingleton<ISpecFileRunner, SpecFileRunner>();

        // Built-in formatters
        services.AddSingleton<ICliFormatterRegistry, CliFormatterRegistry>();

        return services;
    }

    /// <summary>
    /// Adds plugin discovery from the specified directories.
    /// </summary>
    public static IServiceCollection AddPluginDiscovery(
        this IServiceCollection services,
        params string[] pluginDirectories)
    {
        services.AddSingleton<IPluginLoader>(sp =>
            new PluginLoader(pluginDirectories));

        return services;
    }
}

/// <summary>
/// Registry for CLI formatters with factory-based lookup.
/// </summary>
/// <remarks>
/// This interface differs from <see cref="DraftSpec.Plugins.IFormatterRegistry"/> in that it uses
/// a factory pattern to support CLI-specific options (like CSS URL for HTML output).
/// The core library's IFormatterRegistry takes formatter instances directly and is used
/// by the scripting API and plugins. This CLI version needs access to <see cref="CliOptions"/>
/// to configure formatters at resolution time.
/// </remarks>
public interface ICliFormatterRegistry
{
    /// <summary>
    /// Gets a formatter by name, optionally configured with CLI options.
    /// </summary>
    /// <param name="name">The formatter name (e.g., "json", "html", "markdown").</param>
    /// <param name="options">Optional CLI options to configure the formatter.</param>
    /// <returns>The configured formatter, or null if not found.</returns>
    IFormatter? GetFormatter(string name, CliOptions? options = null);

    /// <summary>
    /// Registers a formatter factory with the given name.
    /// </summary>
    /// <param name="name">The formatter name.</param>
    /// <param name="factory">A factory function that creates the formatter with optional CLI options.</param>
    void Register(string name, Func<CliOptions?, IFormatter> factory);

    /// <summary>
    /// Gets all registered formatter names.
    /// </summary>
    IEnumerable<string> Names { get; }
}

/// <summary>
/// Default implementation of <see cref="ICliFormatterRegistry"/> with built-in formatters.
/// </summary>
public class CliFormatterRegistry : ICliFormatterRegistry
{
    private readonly Dictionary<string, Func<CliOptions?, IFormatter>> _factories =
        new(StringComparer.OrdinalIgnoreCase);

    public CliFormatterRegistry()
    {
        // Register built-in formatters
        Register(OutputFormats.Json, _ => new JsonFormatter());
        Register(OutputFormats.Markdown, _ => new MarkdownFormatter());
        Register(OutputFormats.Html, opts => new HtmlFormatter(new HtmlOptions
        {
            CssUrl = opts?.CssUrl ?? "https://cdnjs.cloudflare.com/ajax/libs/simpledotcss/2.3.7/simple.min.css"
        }));
    }

    public IFormatter? GetFormatter(string name, CliOptions? options = null)
    {
        return _factories.TryGetValue(name, out var factory) ? factory(options) : null;
    }

    public void Register(string name, Func<CliOptions?, IFormatter> factory)
    {
        _factories[name] = factory;
    }

    public IEnumerable<string> Names => _factories.Keys;
}
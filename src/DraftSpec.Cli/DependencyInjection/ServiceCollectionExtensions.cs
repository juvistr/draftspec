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
        services.AddSingleton<IFormatterRegistry, FormatterRegistry>();

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
/// Registry for formatters with name-based lookup.
/// </summary>
public interface IFormatterRegistry
{
    /// <summary>
    /// Gets a formatter by name.
    /// </summary>
    IFormatter? GetFormatter(string name, CliOptions? options = null);

    /// <summary>
    /// Registers a formatter with the given name.
    /// </summary>
    void Register(string name, Func<CliOptions?, IFormatter> factory);

    /// <summary>
    /// Gets all registered formatter names.
    /// </summary>
    IEnumerable<string> Names { get; }
}

/// <summary>
/// Default implementation of IFormatterRegistry with built-in formatters.
/// </summary>
public class FormatterRegistry : IFormatterRegistry
{
    private readonly Dictionary<string, Func<CliOptions?, IFormatter>> _factories = new(StringComparer.OrdinalIgnoreCase);

    public FormatterRegistry()
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

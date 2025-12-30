using DraftSpec.Formatters;
using DraftSpec.Formatters.Html;
using DraftSpec.Formatters.JUnit;
using DraftSpec.Formatters.Markdown;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="ICliFormatterRegistry"/> with built-in formatters.
/// </summary>
public class CliFormatterRegistry : ICliFormatterRegistry
{
    private readonly Dictionary<string, Func<string?, IFormatter>> _factories =
        new(StringComparer.OrdinalIgnoreCase);

    public CliFormatterRegistry()
    {
        // Register built-in formatters
        Register(OutputFormats.Json, _ => new JsonFormatter());
        Register(OutputFormats.Markdown, _ => new MarkdownFormatter());
        Register(OutputFormats.Html, cssUrl => new HtmlFormatter(new HtmlOptions
        {
            CssUrl = cssUrl ?? "https://cdnjs.cloudflare.com/ajax/libs/simpledotcss/2.3.7/simple.min.css"
        }));
        Register(OutputFormats.JUnit, _ => new JUnitFormatter());
    }

    public IFormatter? GetFormatter(string name, string? cssUrl = null)
    {
        return _factories.TryGetValue(name, out var factory) ? factory(cssUrl) : null;
    }

    public void Register(string name, Func<string?, IFormatter> factory)
    {
        _factories[name] = factory;
    }

    public IEnumerable<string> Names => _factories.Keys;
}

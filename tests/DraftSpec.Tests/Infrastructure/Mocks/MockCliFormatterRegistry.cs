using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ICliFormatterRegistry for testing.
/// </summary>
public class MockCliFormatterRegistry : ICliFormatterRegistry
{
    private readonly Dictionary<string, Func<string?, IFormatter>> _factories = new();
    private IFormatter? _defaultFormatter;

    public List<(string Name, string? CssUrl)> GetFormatterCalls { get; } = [];

    /// <summary>
    /// Configure a formatter to return for a specific name.
    /// </summary>
    public MockCliFormatterRegistry WithFormatter(string name, IFormatter formatter)
    {
        _factories[name] = _ => formatter;
        return this;
    }

    /// <summary>
    /// Configure a default formatter to return for any name.
    /// </summary>
    public MockCliFormatterRegistry WithDefaultFormatter(IFormatter formatter)
    {
        _defaultFormatter = formatter;
        return this;
    }

    /// <summary>
    /// Configure to return null for unknown formatters.
    /// </summary>
    public MockCliFormatterRegistry ReturnsNullForUnknown()
    {
        _defaultFormatter = null;
        return this;
    }

    public IFormatter? GetFormatter(string name, string? cssUrl = null)
    {
        GetFormatterCalls.Add((name, cssUrl));

        if (_factories.TryGetValue(name, out var factory))
            return factory(cssUrl);

        return _defaultFormatter;
    }

    public void Register(string name, Func<string?, IFormatter> factory)
    {
        _factories[name] = factory;
    }

    public IEnumerable<string> Names => _factories.Keys;
}

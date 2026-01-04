using DraftSpec.Cli;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock runner factory that creates and tracks MockRunner instances.
/// Captures all filter parameters passed during creation.
/// </summary>
public class MockRunnerFactory : IInProcessSpecRunnerFactory
{
    private readonly MockRunner? _runner;
    private readonly Func<MockRunner>? _factory;

    public MockRunnerFactory(MockRunner? runner = null) => _runner = runner;

    /// <summary>
    /// Creates a factory that uses the provided function to create runners.
    /// Useful for tests that need different runner behavior on each call.
    /// </summary>
    public MockRunnerFactory(Func<MockRunner> factory) => _factory = factory;

    // Captured filter parameters
    public string? LastFilterTags { get; private set; }
    public string? LastExcludeTags { get; private set; }
    public string? LastFilterName { get; private set; }
    public string? LastExcludeName { get; private set; }
    public IReadOnlyList<string>? LastFilterContext { get; private set; }
    public IReadOnlyList<string>? LastExcludeContext { get; private set; }

    public IInProcessSpecRunner Create(
        string? filterTags = null,
        string? excludeTags = null,
        string? filterName = null,
        string? excludeName = null,
        IReadOnlyList<string>? filterContext = null,
        IReadOnlyList<string>? excludeContext = null)
    {
        LastFilterTags = filterTags;
        LastExcludeTags = excludeTags;
        LastFilterName = filterName;
        LastExcludeName = excludeName;
        LastFilterContext = filterContext;
        LastExcludeContext = excludeContext;

        if (_factory != null)
            return _factory();

        return _runner ?? new MockRunner();
    }
}

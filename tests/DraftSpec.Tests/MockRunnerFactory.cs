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

    public MockRunnerFactory(MockRunner? runner = null) => _runner = runner;

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
        return _runner ?? new MockRunner();
    }
}

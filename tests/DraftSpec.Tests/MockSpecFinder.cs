using DraftSpec.Cli;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock spec finder for unit testing.
/// Returns a configured list of spec files.
/// </summary>
public class MockSpecFinder : ISpecFinder
{
    private readonly IReadOnlyList<string> _specs;

    public MockSpecFinder(IReadOnlyList<string> specs) => _specs = specs;
    public MockSpecFinder(params string[] specs) => _specs = specs;

    public IReadOnlyList<string> FindSpecs(string path, string? baseDirectory = null) => _specs;
}

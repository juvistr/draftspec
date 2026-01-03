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
    private Exception? _exception;

    public MockSpecFinder(IReadOnlyList<string> specs) => _specs = specs;
    public MockSpecFinder(params string[] specs) => _specs = specs;

    /// <summary>
    /// Configures the mock to throw an exception when FindSpecs is called.
    /// </summary>
    public MockSpecFinder Throws(Exception exception)
    {
        _exception = exception;
        return this;
    }

    public IReadOnlyList<string> FindSpecs(string path, string? baseDirectory = null)
    {
        if (_exception is not null)
            throw _exception;
        return _specs;
    }
}

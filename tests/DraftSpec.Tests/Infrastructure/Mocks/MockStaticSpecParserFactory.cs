using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock factory for creating mock static spec parsers in tests.
/// </summary>
public class MockStaticSpecParserFactory : IStaticSpecParserFactory
{
    private readonly MockStaticSpecParser _mockParser;

    /// <summary>
    /// Tracks calls to <see cref="Create"/>.
    /// </summary>
    public List<(string BaseDirectory, bool UseCache)> CreateCalls { get; } = [];

    /// <summary>
    /// Create a factory that returns a mock parser with configured results.
    /// </summary>
    public MockStaticSpecParserFactory()
    {
        _mockParser = new MockStaticSpecParser();
    }

    /// <summary>
    /// Create a factory that returns the specified mock parser.
    /// </summary>
    public MockStaticSpecParserFactory(MockStaticSpecParser mockParser)
    {
        _mockParser = mockParser;
    }

    /// <summary>
    /// Gets the mock parser returned by this factory.
    /// </summary>
    public MockStaticSpecParser Parser => _mockParser;

    /// <inheritdoc />
    public IStaticSpecParser Create(string baseDirectory, bool useCache = true)
    {
        CreateCalls.Add((baseDirectory, useCache));
        return _mockParser;
    }
}

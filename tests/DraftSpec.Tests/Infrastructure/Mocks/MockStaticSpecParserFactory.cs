using DraftSpec.Cli.Services;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock factory for creating mock static spec parsers in tests.
/// </summary>
public class MockStaticSpecParserFactory : IStaticSpecParserFactory
{
    private readonly MockStaticSpecParser _mockParser;
    private Exception? _createException;
    private int _specCount;
    private string[]? _defaultWarnings;
    private Exception? _parseException;

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

    /// <summary>
    /// Configure the factory to throw an exception when <see cref="Create"/> is called.
    /// </summary>
    public MockStaticSpecParserFactory WithCreateException(Exception exception)
    {
        _createException = exception;
        return this;
    }

    /// <summary>
    /// Configure the default number of specs to return for any file.
    /// </summary>
    public MockStaticSpecParserFactory WithSpecCount(int count)
    {
        _specCount = count;
        return this;
    }

    /// <summary>
    /// Configure default warnings to return for any file.
    /// </summary>
    public MockStaticSpecParserFactory WithWarnings(params string[] warnings)
    {
        _defaultWarnings = warnings;
        return this;
    }

    /// <summary>
    /// Configure the parser to throw on any parse.
    /// </summary>
    public MockStaticSpecParserFactory ThrowsOnParse(Exception exception)
    {
        _parseException = exception;
        return this;
    }

    /// <inheritdoc />
    public IStaticSpecParser Create(string baseDirectory, bool useCache = true)
    {
        CreateCalls.Add((baseDirectory, useCache));

        if (_createException != null)
        {
            throw _createException;
        }

        // Configure the mock parser with defaults if set
        if (_specCount > 0 || _defaultWarnings != null || _parseException != null)
        {
            return new ConfiguredMockParser(_specCount, _defaultWarnings, _parseException);
        }

        return _mockParser;
    }

    /// <summary>
    /// A mock parser that returns configured defaults for any file.
    /// </summary>
    private class ConfiguredMockParser : IStaticSpecParser
    {
        private readonly int _specCount;
        private readonly string[]? _warnings;
        private readonly Exception? _exception;

        public ConfiguredMockParser(int specCount, string[]? warnings, Exception? exception)
        {
            _specCount = specCount;
            _warnings = warnings;
            _exception = exception;
        }

        public Task<StaticParseResult> ParseFileAsync(string csxFilePath, CancellationToken cancellationToken = default)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            var specs = Enumerable.Range(1, _specCount)
                .Select(i => new StaticSpec
                {
                    Description = $"spec{i}",
                    ContextPath = ["Context"],
                    LineNumber = i,
                    Type = StaticSpecType.Regular
                })
                .ToList();

            return Task.FromResult(new StaticParseResult
            {
                Specs = specs,
                Warnings = _warnings ?? []
            });
        }
    }
}

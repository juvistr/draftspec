using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Default implementation of <see cref="IStaticSpecParserFactory"/>.
/// </summary>
public class StaticSpecParserFactory : IStaticSpecParserFactory
{
    /// <inheritdoc />
    public IStaticSpecParser Create(string baseDirectory, bool useCache = true)
    {
        return new StaticSpecParserWrapper(new StaticSpecParser(baseDirectory, useCache));
    }

    private class StaticSpecParserWrapper : IStaticSpecParser
    {
        private readonly StaticSpecParser _inner;

        public StaticSpecParserWrapper(StaticSpecParser inner)
        {
            _inner = inner;
        }

        public Task<StaticParseResult> ParseFileAsync(string csxFilePath, CancellationToken cancellationToken = default)
        {
            return _inner.ParseFileAsync(csxFilePath, cancellationToken);
        }
    }
}

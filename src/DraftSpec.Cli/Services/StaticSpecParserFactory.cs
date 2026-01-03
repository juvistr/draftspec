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
        return new StaticSpecParser(baseDirectory, useCache);
    }
}

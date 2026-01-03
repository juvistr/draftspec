using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Factory for creating <see cref="IStaticSpecParser"/> instances.
/// </summary>
/// <remarks>
/// Allows phases to create parsers with the correct base directory,
/// which is only known at runtime after path resolution.
/// </remarks>
public interface IStaticSpecParserFactory
{
    /// <summary>
    /// Create a new static spec parser for the given base directory.
    /// </summary>
    /// <param name="baseDirectory">Base directory for resolving #load directives.</param>
    /// <param name="useCache">Whether to use disk caching for parse results.</param>
    /// <returns>A new parser instance.</returns>
    IStaticSpecParser Create(string baseDirectory, bool useCache = true);
}

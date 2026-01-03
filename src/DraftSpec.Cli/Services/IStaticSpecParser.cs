using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Interface for static spec parsing operations.
/// </summary>
/// <remarks>
/// Wraps <see cref="StaticSpecParser"/> to enable testing.
/// </remarks>
public interface IStaticSpecParser
{
    /// <summary>
    /// Parse a spec file and return discovered specs.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX spec file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parse result containing specs and warnings.</returns>
    Task<StaticParseResult> ParseFileAsync(string csxFilePath, CancellationToken cancellationToken = default);
}

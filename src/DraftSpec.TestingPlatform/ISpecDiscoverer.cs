namespace DraftSpec.TestingPlatform;

/// <summary>
/// Abstraction for discovering specs from CSX files.
/// Enables deterministic testing of discovery orchestration.
/// </summary>
public interface ISpecDiscoverer
{
    /// <summary>
    /// Discovers all specs from CSX files in the project directory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovery result containing specs and any errors.</returns>
    Task<SpecDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers specs from a single CSX file.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of discovered specs from the file.</returns>
    Task<IReadOnlyList<DiscoveredSpec>> DiscoverFileAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default);
}

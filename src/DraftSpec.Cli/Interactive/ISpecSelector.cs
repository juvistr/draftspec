using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Interactive;

/// <summary>
/// Provides interactive spec selection capabilities.
/// </summary>
public interface ISpecSelector
{
    /// <summary>
    /// Displays an interactive selection UI and returns selected specs.
    /// </summary>
    /// <param name="specs">All discovered specs to choose from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Selection result with chosen specs or cancellation status.</returns>
    Task<SpecSelectionResult> SelectAsync(
        IReadOnlyList<DiscoveredSpec> specs,
        CancellationToken ct = default);
}

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Result of spec discovery, containing both discovered specs and any errors.
/// </summary>
public sealed class DiscoveryResult
{
    /// <summary>
    /// Successfully discovered specs.
    /// </summary>
    public IReadOnlyList<DiscoveredSpec> Specs { get; init; } = [];

    /// <summary>
    /// Errors that occurred during discovery.
    /// </summary>
    public IReadOnlyList<DiscoveryError> Errors { get; init; } = [];

    /// <summary>
    /// True if there were any discovery errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Total count of specs plus errors (for UI display).
    /// </summary>
    public int TotalCount => Specs.Count + Errors.Count;
}

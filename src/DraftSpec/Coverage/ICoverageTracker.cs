namespace DraftSpec.Coverage;

/// <summary>
/// Interface for tracking code coverage during spec execution.
/// Implementations can use various coverage tools (Coverlet, dotnet-coverage, etc.).
/// </summary>
/// <remarks>
/// The tracker uses a snapshot/diff pattern:
/// 1. Call <see cref="TakeSnapshot"/> before spec execution
/// 2. Execute the spec
/// 3. Call <see cref="GetCoverageSince"/> to get the delta
///
/// Thread-safe implementations should be used for parallel spec execution.
/// </remarks>
public interface ICoverageTracker : IDisposable
{
    /// <summary>
    /// Whether coverage tracking is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Start coverage collection if not already started.
    /// Called once before running specs.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop coverage collection.
    /// Called after all specs have completed.
    /// </summary>
    void StopTracking();

    /// <summary>
    /// Take a snapshot of current coverage state.
    /// Returns a snapshot ID that can be used with <see cref="GetCoverageSince"/>.
    /// </summary>
    /// <returns>Snapshot identifier for use with GetCoverageSince</returns>
    CoverageSnapshot TakeSnapshot();

    /// <summary>
    /// Get the coverage delta since a snapshot was taken.
    /// Returns only the lines that were newly covered since the snapshot.
    /// </summary>
    /// <param name="snapshot">Snapshot taken before spec execution</param>
    /// <param name="specId">Identifier for the spec (used in returned data)</param>
    /// <returns>Coverage data for lines covered since the snapshot</returns>
    SpecCoverageData GetCoverageSince(CoverageSnapshot snapshot, string specId);

    /// <summary>
    /// Get the total accumulated coverage from all specs.
    /// </summary>
    /// <returns>Dictionary of file path to line coverage</returns>
    IReadOnlyDictionary<string, CoveredFile> GetTotalCoverage();
}

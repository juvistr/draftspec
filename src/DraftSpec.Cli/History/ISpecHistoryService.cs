namespace DraftSpec.Cli.History;

/// <summary>
/// Service for managing spec execution history for flaky test detection.
/// </summary>
public interface ISpecHistoryService
{
    /// <summary>
    /// Loads the history file from the project directory.
    /// Returns empty history if the file doesn't exist or is corrupt.
    /// </summary>
    /// <param name="projectPath">Path to the project root directory.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The spec history, or empty history if not found.</returns>
    Task<SpecHistory> LoadAsync(string projectPath, CancellationToken ct = default);

    /// <summary>
    /// Saves the history file atomically to the project directory.
    /// Creates the .draftspec directory if it doesn't exist.
    /// </summary>
    /// <param name="projectPath">Path to the project root directory.</param>
    /// <param name="history">The history to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(string projectPath, SpecHistory history, CancellationToken ct = default);

    /// <summary>
    /// Records results from a test run, updating history.
    /// Automatically trims old runs to prevent unbounded growth.
    /// </summary>
    /// <param name="projectPath">Path to the project root directory.</param>
    /// <param name="results">The spec run results to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordRunAsync(
        string projectPath,
        IReadOnlyList<SpecRunRecord> results,
        CancellationToken ct = default);

    /// <summary>
    /// Detects flaky specs based on status transitions in recent runs.
    /// </summary>
    /// <param name="history">The spec history to analyze.</param>
    /// <param name="minStatusChanges">Minimum status transitions to be considered flaky (default: 2).</param>
    /// <param name="windowSize">Number of recent runs to analyze (default: 10).</param>
    /// <returns>List of flaky specs, ordered by severity (most flaky first).</returns>
    IReadOnlyList<FlakySpec> GetFlakySpecs(
        SpecHistory history,
        int minStatusChanges = 2,
        int windowSize = 10);

    /// <summary>
    /// Gets spec IDs that should be quarantined (skipped) during runs.
    /// </summary>
    /// <param name="history">The spec history to analyze.</param>
    /// <param name="minStatusChanges">Minimum status transitions to quarantine (default: 2).</param>
    /// <param name="windowSize">Number of recent runs to analyze (default: 10).</param>
    /// <returns>Set of spec IDs to quarantine.</returns>
    IReadOnlySet<string> GetQuarantinedSpecIds(
        SpecHistory history,
        int minStatusChanges = 2,
        int windowSize = 10);

    /// <summary>
    /// Clears history for a specific spec.
    /// </summary>
    /// <param name="projectPath">Path to the project root directory.</param>
    /// <param name="specId">The spec ID to clear.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the spec was found and cleared, false otherwise.</returns>
    Task<bool> ClearSpecAsync(string projectPath, string specId, CancellationToken ct = default);
}

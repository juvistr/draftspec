namespace DraftSpec.TestingPlatform;

/// <summary>
/// Abstraction for executing specs for MTP integration.
/// Enables deterministic testing of execution orchestration.
/// </summary>
internal interface IMtpSpecExecutor
{
    /// <summary>
    /// Executes all specs from a CSX file and returns results.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results paired with relative source file path.</returns>
    Task<ExecutionResult> ExecuteFileAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes filtered specs from a CSX file and returns results.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX file.</param>
    /// <param name="requestedIds">Set of spec IDs to run, or null to run all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results paired with relative source file path.</returns>
    Task<ExecutionResult> ExecuteFileAsync(
        string csxFilePath,
        HashSet<string>? requestedIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes specs from multiple files based on requested test IDs.
    /// Groups IDs by file and executes each file with its relevant IDs.
    /// </summary>
    /// <param name="requestedIds">Set of spec IDs to run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results from all files.</returns>
    Task<IReadOnlyList<ExecutionResult>> ExecuteByIdsAsync(
        IEnumerable<string> requestedIds,
        CancellationToken cancellationToken = default);
}

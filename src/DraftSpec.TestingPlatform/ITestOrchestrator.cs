namespace DraftSpec.TestingPlatform;

/// <summary>
/// Orchestrates test discovery and execution workflows.
/// Abstracts the core orchestration logic from the test framework adapter for testability.
/// </summary>
internal interface ITestOrchestrator
{
    /// <summary>
    /// Discovers all tests and publishes them to the test runner.
    /// </summary>
    /// <param name="publisher">The publisher to send test nodes to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DiscoverTestsAsync(
        ITestNodePublisher publisher,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs tests and publishes results to the test runner.
    /// </summary>
    /// <param name="requestedTestIds">Optional set of test IDs to run. If null, runs all tests.</param>
    /// <param name="publisher">The publisher to send test nodes to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RunTestsAsync(
        IReadOnlySet<string>? requestedTestIds,
        ITestNodePublisher publisher,
        CancellationToken cancellationToken = default);
}

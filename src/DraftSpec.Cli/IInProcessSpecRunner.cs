namespace DraftSpec.Cli;

/// <summary>
/// Runs spec files in-process and returns results.
/// </summary>
public interface IInProcessSpecRunner
{
    /// <summary>
    /// Event raised when a project build starts.
    /// </summary>
    event Action<string>? OnBuildStarted;

    /// <summary>
    /// Event raised when a project build completes.
    /// </summary>
    event Action<BuildResult>? OnBuildCompleted;

    /// <summary>
    /// Event raised when a project build is skipped (no changes detected).
    /// </summary>
    event Action<string>? OnBuildSkipped;

    /// <summary>
    /// Run a single spec file and return the report.
    /// </summary>
    Task<InProcessRunResult> RunFileAsync(string specFile, CancellationToken ct = default);

    /// <summary>
    /// Run all spec files, optionally in parallel.
    /// </summary>
    Task<InProcessRunSummary> RunAllAsync(
        IReadOnlyList<string> specFiles,
        bool parallel = false,
        CancellationToken ct = default);

    /// <summary>
    /// Clear the build cache to force rebuilds.
    /// </summary>
    void ClearBuildCache();
}

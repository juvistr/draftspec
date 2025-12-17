namespace DraftSpec.Cli;

/// <summary>
/// Runs spec files and manages project builds.
/// </summary>
public interface ISpecFileRunner
{
    /// <summary>
    /// Fired when a project build starts.
    /// </summary>
    event Action<string>? OnBuildStarted;

    /// <summary>
    /// Fired when a project build completes.
    /// </summary>
    event Action<BuildResult>? OnBuildCompleted;

    /// <summary>
    /// Fired when a project build is skipped (no source changes).
    /// </summary>
    event Action<string>? OnBuildSkipped;

    /// <summary>
    /// Run a single spec file, building projects if needed.
    /// </summary>
    /// <param name="specFile">Path to .spec.csx file</param>
    /// <returns>Spec run result with output, error, and exit code</returns>
    SpecRunResult Run(string specFile);

    /// <summary>
    /// Run multiple spec files, optionally in parallel.
    /// Builds each unique directory once before running specs.
    /// </summary>
    /// <param name="specFiles">Paths to .spec.csx files</param>
    /// <param name="parallel">Whether to run in parallel (default: false)</param>
    /// <returns>Summary with all results and total duration</returns>
    RunSummary RunAll(IReadOnlyList<string> specFiles, bool parallel = false);

    /// <summary>
    /// Run a single spec file with JSON output mode by modifying the run() call.
    /// </summary>
    /// <param name="specFile">Path to .spec.csx file</param>
    /// <returns>Spec run result with JSON output</returns>
    SpecRunResult RunWithJson(string specFile);

    /// <summary>
    /// Run a single spec file with JSON output via FileReporter mechanism.
    /// Separates JSON (to temp file) from console output (stdout).
    /// </summary>
    /// <param name="specFile">Path to .spec.csx file</param>
    /// <returns>Spec run result with clean JSON output</returns>
    SpecRunResult RunWithJsonReporter(string specFile);

    /// <summary>
    /// Clear the build cache, forcing a full rebuild on next run.
    /// </summary>
    void ClearBuildCache();
}
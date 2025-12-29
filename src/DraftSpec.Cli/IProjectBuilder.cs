namespace DraftSpec.Cli;

/// <summary>
/// Discovers and builds .NET projects for spec execution.
/// </summary>
public interface IProjectBuilder
{
    /// <summary>
    /// Event raised when a build starts.
    /// </summary>
    event Action<string>? OnBuildStarted;

    /// <summary>
    /// Event raised when a build completes.
    /// </summary>
    event Action<BuildResult>? OnBuildCompleted;

    /// <summary>
    /// Event raised when a build is skipped (no changes detected).
    /// </summary>
    event Action<string>? OnBuildSkipped;

    /// <summary>
    /// Build projects in the given directory.
    /// </summary>
    /// <param name="directory">The directory containing specs.</param>
    void BuildProjects(string directory);

    /// <summary>
    /// Find the output directory for assemblies (e.g., bin/Debug/net10.0).
    /// </summary>
    /// <param name="specDirectory">The directory containing specs.</param>
    /// <returns>The path to the output directory.</returns>
    string FindOutputDirectory(string specDirectory);

    /// <summary>
    /// Clear the build cache to force rebuilds.
    /// </summary>
    void ClearBuildCache();
}

namespace DraftSpec.Cli;

/// <summary>
/// Tracks build times to enable incremental builds.
/// </summary>
public interface IBuildCache
{
    /// <summary>
    /// Check if a rebuild is needed for the given directory.
    /// </summary>
    /// <param name="directory">The project directory.</param>
    /// <param name="latestSourceModification">The most recent source file modification time.</param>
    /// <returns>True if a rebuild is needed.</returns>
    bool NeedsRebuild(string directory, DateTime latestSourceModification);

    /// <summary>
    /// Update the cache after a successful build.
    /// </summary>
    /// <param name="directory">The project directory.</param>
    /// <param name="buildTime">The time of the build.</param>
    /// <param name="sourceModification">The most recent source modification at build time.</param>
    void UpdateCache(string directory, DateTime buildTime, DateTime sourceModification);

    /// <summary>
    /// Clear all cached build information.
    /// </summary>
    void Clear();
}

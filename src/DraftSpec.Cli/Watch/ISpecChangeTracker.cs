using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Watch;

/// <summary>
/// Tracks spec state across file changes for incremental watch mode.
/// </summary>
public interface ISpecChangeTracker
{
    /// <summary>
    /// Records the current state of a spec file.
    /// </summary>
    /// <param name="filePath">The path to the spec file.</param>
    /// <param name="parseResult">The static parse result for the file.</param>
    void RecordState(string filePath, StaticParseResult parseResult);

    /// <summary>
    /// Gets changes between the recorded state and new parse result.
    /// </summary>
    /// <param name="filePath">The path to the spec file.</param>
    /// <param name="newResult">The new parse result to compare against recorded state.</param>
    /// <param name="dependencyChanged">Whether a dependency (like spec_helper.csx) changed.</param>
    /// <returns>A SpecChangeSet containing detected changes.</returns>
    SpecChangeSet GetChanges(string filePath, StaticParseResult newResult, bool dependencyChanged);

    /// <summary>
    /// Checks if there is a recorded state for the given file.
    /// </summary>
    /// <param name="filePath">The path to the spec file.</param>
    /// <returns>True if state was previously recorded for this file.</returns>
    bool HasState(string filePath);

    /// <summary>
    /// Clears all tracked state.
    /// </summary>
    void Clear();

    /// <summary>
    /// Records a dependency's modification time.
    /// </summary>
    /// <param name="dependencyPath">The path to the dependency file.</param>
    /// <param name="lastModified">The last modification time.</param>
    void RecordDependency(string dependencyPath, DateTime lastModified);

    /// <summary>
    /// Checks if a dependency has changed since last recorded.
    /// </summary>
    /// <param name="dependencyPath">The path to the dependency file.</param>
    /// <param name="currentModified">The current modification time.</param>
    /// <returns>True if the dependency has changed or was never recorded.</returns>
    bool HasDependencyChanged(string dependencyPath, DateTime currentModified);
}

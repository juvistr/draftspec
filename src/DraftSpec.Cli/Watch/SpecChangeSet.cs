namespace DraftSpec.Cli.Watch;

/// <summary>
/// Collection of spec changes for a file.
/// </summary>
/// <param name="FilePath">The path to the spec file.</param>
/// <param name="Changes">The individual spec changes.</param>
/// <param name="HasDynamicSpecs">Whether the file contains dynamic specs that prevent incremental runs.</param>
/// <param name="DependencyChanged">Whether a dependency (like spec_helper.csx) changed.</param>
public sealed record SpecChangeSet(
    string FilePath,
    IReadOnlyList<SpecChange> Changes,
    bool HasDynamicSpecs,
    bool DependencyChanged)
{
    /// <summary>
    /// Whether a full file run is required instead of incremental.
    /// </summary>
    public bool RequiresFullRun => HasDynamicSpecs || DependencyChanged;

    /// <summary>
    /// Whether there are any changes to process.
    /// </summary>
    public bool HasChanges => Changes.Count > 0 || RequiresFullRun;

    /// <summary>
    /// Gets the specs that need to be run (excludes deleted specs).
    /// </summary>
    public IReadOnlyList<SpecChange> SpecsToRun => Changes
        .Where(c => c.ChangeType != SpecChangeType.Deleted)
        .ToList();
}

namespace DraftSpec.Cli.Watch;

/// <summary>
/// Type of change detected for a spec.
/// </summary>
public enum SpecChangeType
{
    /// <summary>
    /// A new spec was added to the file.
    /// </summary>
    Added,

    /// <summary>
    /// An existing spec was modified (line number, type, or pending status changed).
    /// </summary>
    Modified,

    /// <summary>
    /// A spec was removed from the file.
    /// </summary>
    Deleted
}

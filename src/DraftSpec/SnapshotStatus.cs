namespace DraftSpec.Snapshots;

/// <summary>
/// Status of a snapshot comparison.
/// </summary>
public enum SnapshotStatus
{
    /// <summary>Actual value matches stored snapshot.</summary>
    Matched,

    /// <summary>Actual value differs from stored snapshot.</summary>
    Mismatched,

    /// <summary>New snapshot was created (first run or update mode).</summary>
    Created,

    /// <summary>Existing snapshot was updated (update mode).</summary>
    Updated,

    /// <summary>Snapshot does not exist and update mode is off.</summary>
    Missing
}

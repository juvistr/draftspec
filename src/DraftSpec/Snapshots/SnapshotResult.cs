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

/// <summary>
/// Result of a snapshot comparison operation.
/// </summary>
/// <param name="Status">The comparison status</param>
/// <param name="Key">The snapshot key (description or custom name)</param>
/// <param name="Expected">Expected value from snapshot (if mismatched)</param>
/// <param name="Actual">Actual serialized value (if mismatched)</param>
/// <param name="Diff">Human-readable diff (if mismatched)</param>
public record SnapshotResult(
    SnapshotStatus Status,
    string Key,
    string? Expected = null,
    string? Actual = null,
    string? Diff = null)
{
    /// <summary>Create a matched result.</summary>
    public static SnapshotResult Matched(string key) =>
        new(SnapshotStatus.Matched, key);

    /// <summary>Create a mismatched result with diff.</summary>
    public static SnapshotResult Mismatched(string key, string expected, string actual, string diff) =>
        new(SnapshotStatus.Mismatched, key, expected, actual, diff);

    /// <summary>Create a created result for new snapshots.</summary>
    public static SnapshotResult Created(string key) =>
        new(SnapshotStatus.Created, key);

    /// <summary>Create an updated result for updated snapshots.</summary>
    public static SnapshotResult Updated(string key) =>
        new(SnapshotStatus.Updated, key);

    /// <summary>Create a missing result when snapshot doesn't exist.</summary>
    public static SnapshotResult Missing(string key) =>
        new(SnapshotStatus.Missing, key);
}

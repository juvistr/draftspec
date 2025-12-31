namespace DraftSpec.Snapshots;

/// <summary>
/// Holds the current spec execution context for snapshot assertions.
/// Uses AsyncLocal for thread-safe access during spec execution.
/// </summary>
public static class SnapshotContext
{
    private static readonly AsyncLocal<SnapshotInfo?> CurrentLocal = new();

    /// <summary>
    /// Current snapshot info for the executing spec.
    /// Null when not inside an it() block.
    /// </summary>
    public static SnapshotInfo? Current
    {
        get => CurrentLocal.Value;
        internal set => CurrentLocal.Value = value;
    }
}

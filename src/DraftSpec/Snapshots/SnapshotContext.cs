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

/// <summary>
/// Information needed to identify and store a snapshot.
/// </summary>
/// <param name="FullDescription">Complete description including all context paths</param>
/// <param name="ContextPath">List of describe/context block names</param>
/// <param name="SpecDescription">The it() block description</param>
public record SnapshotInfo(
    string FullDescription,
    IReadOnlyList<string> ContextPath,
    string SpecDescription);

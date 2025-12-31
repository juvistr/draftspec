namespace DraftSpec.Snapshots;

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

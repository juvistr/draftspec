namespace DraftSpec.Coverage;

/// <summary>
/// Represents a point-in-time snapshot of coverage state.
/// Used to calculate coverage deltas between spec executions.
/// </summary>
public sealed class CoverageSnapshot
{
    /// <summary>
    /// Unique identifier for this snapshot.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// When the snapshot was taken.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Internal state captured at snapshot time.
    /// Implementation-specific; may be null for stateless implementations.
    /// </summary>
    internal object? State { get; init; }
}

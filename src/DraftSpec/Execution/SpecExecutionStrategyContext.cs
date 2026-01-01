namespace DraftSpec.Execution;

/// <summary>
/// Context passed to execution strategies containing specs to execute and callbacks.
/// </summary>
public sealed class SpecExecutionStrategyContext
{
    /// <summary>
    /// The specs to execute.
    /// </summary>
    public required IReadOnlyList<SpecDefinition> Specs { get; init; }

    /// <summary>
    /// The spec context containing hooks and metadata.
    /// </summary>
    public required SpecContext Context { get; init; }

    /// <summary>
    /// Snapshot of the context path for creating results.
    /// </summary>
    public required IReadOnlyList<string> ContextPath { get; init; }

    /// <summary>
    /// The list to add results to (in order).
    /// </summary>
    public required List<SpecResult> Results { get; init; }

    /// <summary>
    /// Whether any spec in the tree is focused.
    /// </summary>
    public required bool HasFocused { get; init; }

    /// <summary>
    /// Callback to run a single spec through the pipeline.
    /// </summary>
    public required Func<SpecDefinition, SpecContext, IReadOnlyList<string>, bool, Task<SpecResult>> RunSpec { get; init; }

    /// <summary>
    /// Callback to notify reporters of a single spec completion (used by sequential).
    /// </summary>
    public required Func<SpecResult, Task> NotifyCompleted { get; init; }

    /// <summary>
    /// Callback to notify reporters of batch spec completion (used by parallel).
    /// </summary>
    public required Func<IReadOnlyList<SpecResult>, Task> NotifyBatchCompleted { get; init; }

    /// <summary>
    /// Check if bail has been triggered.
    /// </summary>
    public required Func<bool> IsBailTriggered { get; init; }

    /// <summary>
    /// Signal that bail should be triggered (after a failure when bail mode is enabled).
    /// </summary>
    public required Action SignalBail { get; init; }

    /// <summary>
    /// Whether bail mode is enabled.
    /// </summary>
    public required bool BailEnabled { get; init; }
}

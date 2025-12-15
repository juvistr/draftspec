using System.Collections.Concurrent;

namespace DraftSpec.Middleware;

/// <summary>
/// Context passed through the middleware pipeline.
/// Contains all information needed to execute a spec.
/// Thread-safe for use with parallel execution.
/// </summary>
public class SpecExecutionContext
{
    /// <summary>
    /// The spec definition being executed.
    /// </summary>
    public required SpecDefinition Spec { get; init; }

    /// <summary>
    /// The context containing the spec (for hooks).
    /// </summary>
    public required SpecContext Context { get; init; }

    /// <summary>
    /// Path of context descriptions leading to this spec.
    /// </summary>
    public required IReadOnlyList<string> ContextPath { get; init; }

    /// <summary>
    /// Whether focus mode is active.
    /// </summary>
    public required bool HasFocused { get; init; }

    /// <summary>
    /// Cancellation token for timeout support.
    /// Middleware can set this; specs can check it.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

    /// <summary>
    /// Thread-safe mutable bag for middleware to share state.
    /// Key: middleware type name, Value: arbitrary data.
    /// Uses ConcurrentDictionary for safe parallel access.
    /// </summary>
    public ConcurrentDictionary<string, object> Items { get; } = new();
}

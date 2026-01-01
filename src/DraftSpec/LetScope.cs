namespace DraftSpec;

/// <summary>
/// Holds the execution context for let/get fixtures during spec execution.
/// Uses AsyncLocal for thread-safe access during parallel spec execution.
/// </summary>
/// <remarks>
/// LetScope is created fresh for each spec execution and holds:
/// - Reference to the SpecContext (for finding let definitions)
/// - Memoized values (cached results of factory invocations)
///
/// Values are lazily created on first access via get&lt;T&gt;() and
/// cached for the duration of the spec. Each spec gets a fresh LetScope.
/// </remarks>
public class LetScope
{
    private static readonly AsyncLocal<LetScope?> CurrentLocal = new();

    /// <summary>
    /// Current let scope for the executing spec.
    /// Null when not inside a spec body.
    /// </summary>
    public static LetScope? Current
    {
        get => CurrentLocal.Value;
        internal set => CurrentLocal.Value = value;
    }

    /// <summary>
    /// The context containing let definitions for this scope.
    /// </summary>
    public SpecContext Context { get; }

    /// <summary>
    /// Memoized values created during this spec execution.
    /// </summary>
    internal Dictionary<string, object> Values { get; } = new();

    /// <summary>
    /// Creates a new let scope for the given context.
    /// </summary>
    /// <param name="context">The spec context containing let definitions</param>
    public LetScope(SpecContext context)
    {
        Context = context;
    }
}

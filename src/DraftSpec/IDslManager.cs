namespace DraftSpec;

/// <summary>
/// Manages DSL state for spec execution.
/// Enables testability by abstracting static Dsl.Reset() calls.
/// </summary>
public interface IDslManager
{
    /// <summary>
    /// Reset all DSL state for a clean execution context.
    /// </summary>
    void Reset();
}

/// <summary>
/// Default implementation that delegates to static Dsl.Reset().
/// </summary>
public class DslManager : IDslManager
{
    /// <summary>
    /// Reset DSL state via static Dsl.Reset().
    /// </summary>
    public void Reset() => Dsl.Reset();
}

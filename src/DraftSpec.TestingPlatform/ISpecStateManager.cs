namespace DraftSpec.TestingPlatform;

/// <summary>
/// Manages DSL state for spec discovery and execution.
/// Enables testability by abstracting static Dsl state access.
/// </summary>
/// <remarks>
/// This interface allows:
/// - Unit testing without global state pollution
/// - Verification that state is properly reset between executions
/// - Future support for parallel test execution with isolated state
/// </remarks>
public interface ISpecStateManager
{
    /// <summary>
    /// Resets all DSL state for a clean execution context.
    /// Should be called before and after each spec file execution.
    /// </summary>
    void ResetState();

    /// <summary>
    /// Gets the current root context from DSL state.
    /// </summary>
    /// <returns>The root SpecContext, or null if no specs have been defined.</returns>
    SpecContext? GetRootContext();

    /// <summary>
    /// Sets the root context in DSL state.
    /// Used for testing or manual state manipulation.
    /// </summary>
    /// <param name="context">The context to set as root, or null to clear.</param>
    void SetRootContext(SpecContext? context);
}

namespace DraftSpec.Internal;

/// <summary>
/// Shared context-building logic for both Dsl (static) and Spec (instance-based) APIs.
/// </summary>
internal static class ContextBuilder
{
    /// <summary>
    /// Create a new nested context as a child of the current context.
    /// </summary>
    public static SpecContext CreateNestedContext(string description, SpecContext parent)
    {
        return new SpecContext(description, parent);
    }

    /// <summary>
    /// Add a spec to the current context with validation.
    /// </summary>
    public static void AddSpec(SpecContext? context, SpecDefinition spec)
    {
        EnsureContext(context);
        context!.AddSpec(spec);
    }

    /// <summary>
    /// Create a regular spec definition.
    /// </summary>
    public static SpecDefinition CreateSpec(string description, Action body)
    {
        return new SpecDefinition(description, body);
    }

    /// <summary>
    /// Create a pending spec definition (no body).
    /// </summary>
    public static SpecDefinition CreatePendingSpec(string description)
    {
        return new SpecDefinition(description);
    }

    /// <summary>
    /// Create a focused spec definition.
    /// </summary>
    public static SpecDefinition CreateFocusedSpec(string description, Action body)
    {
        return new SpecDefinition(description, body) { IsFocused = true };
    }

    /// <summary>
    /// Create a skipped spec definition.
    /// </summary>
    public static SpecDefinition CreateSkippedSpec(string description, Action? body)
    {
        return new SpecDefinition(description, body) { IsSkipped = true };
    }

    /// <summary>
    /// Set the beforeEach hook on a context with validation.
    /// </summary>
    public static void SetBeforeEach(SpecContext? context, Action hook)
    {
        EnsureContext(context);
        context!.BeforeEach = hook;
    }

    /// <summary>
    /// Set the afterEach hook on a context with validation.
    /// </summary>
    public static void SetAfterEach(SpecContext? context, Action hook)
    {
        EnsureContext(context);
        context!.AfterEach = hook;
    }

    /// <summary>
    /// Set the beforeAll hook on a context with validation.
    /// </summary>
    public static void SetBeforeAll(SpecContext? context, Action hook)
    {
        EnsureContext(context);
        context!.BeforeAll = hook;
    }

    /// <summary>
    /// Set the afterAll hook on a context with validation.
    /// </summary>
    public static void SetAfterAll(SpecContext? context, Action hook)
    {
        EnsureContext(context);
        context!.AfterAll = hook;
    }

    /// <summary>
    /// Validate that we're inside a describe block.
    /// </summary>
    public static void EnsureContext(SpecContext? context)
    {
        if (context is null)
            throw new InvalidOperationException("Must be called inside a describe() block");
    }
}

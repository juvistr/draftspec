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
    /// Create a regular spec definition with a sync body.
    /// </summary>
    public static SpecDefinition CreateSpec(string description, Action body, IReadOnlyList<string>? tags = null, int lineNumber = 0)
    {
        return new SpecDefinition(description, body) { Tags = tags ?? [], LineNumber = lineNumber };
    }

    /// <summary>
    /// Create a regular spec definition with an async body.
    /// </summary>
    public static SpecDefinition CreateSpec(string description, Func<Task> body, IReadOnlyList<string>? tags = null, int lineNumber = 0)
    {
        return new SpecDefinition(description, body) { Tags = tags ?? [], LineNumber = lineNumber };
    }

    /// <summary>
    /// Create a pending spec definition (no body).
    /// </summary>
    public static SpecDefinition CreatePendingSpec(string description, IReadOnlyList<string>? tags = null, int lineNumber = 0)
    {
        return new SpecDefinition(description) { Tags = tags ?? [], LineNumber = lineNumber };
    }

    /// <summary>
    /// Create a focused spec definition with a sync body.
    /// </summary>
    public static SpecDefinition CreateFocusedSpec(string description, Action body, IReadOnlyList<string>? tags = null, int lineNumber = 0)
    {
        return new SpecDefinition(description, body) { IsFocused = true, Tags = tags ?? [], LineNumber = lineNumber };
    }

    /// <summary>
    /// Create a focused spec definition with an async body.
    /// </summary>
    public static SpecDefinition CreateFocusedSpec(string description, Func<Task> body,
        IReadOnlyList<string>? tags = null, int lineNumber = 0)
    {
        return new SpecDefinition(description, body) { IsFocused = true, Tags = tags ?? [], LineNumber = lineNumber };
    }

    /// <summary>
    /// Create a skipped spec definition with a sync body.
    /// </summary>
    public static SpecDefinition CreateSkippedSpec(string description, Action? body, IReadOnlyList<string>? tags = null, int lineNumber = 0)
    {
        return body == null
            ? new SpecDefinition(description) { IsSkipped = true, Tags = tags ?? [], LineNumber = lineNumber }
            : new SpecDefinition(description, body) { IsSkipped = true, Tags = tags ?? [], LineNumber = lineNumber };
    }

    /// <summary>
    /// Create a skipped spec definition with an async body.
    /// </summary>
    public static SpecDefinition CreateSkippedSpec(string description, Func<Task>? body,
        IReadOnlyList<string>? tags = null, int lineNumber = 0)
    {
        return new SpecDefinition(description, body) { IsSkipped = true, Tags = tags ?? [], LineNumber = lineNumber };
    }

    /// <summary>
    /// Wrap a sync action in a Func&lt;Task&gt; that returns Task.CompletedTask.
    /// </summary>
    public static Func<Task> WrapSync(Action body)
    {
        return () =>
        {
            body();
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Add a beforeEach hook to a context with validation (sync version).
    /// </summary>
    public static void AddBeforeEach(SpecContext? context, Action hook)
    {
        EnsureContext(context);
        context!.AddBeforeEach(WrapSync(hook));
    }

    /// <summary>
    /// Add a beforeEach hook to a context with validation (async version).
    /// </summary>
    public static void AddBeforeEach(SpecContext? context, Func<Task> hook)
    {
        EnsureContext(context);
        context!.AddBeforeEach(hook);
    }

    /// <summary>
    /// Add an afterEach hook to a context with validation (sync version).
    /// </summary>
    public static void AddAfterEach(SpecContext? context, Action hook)
    {
        EnsureContext(context);
        context!.AddAfterEach(WrapSync(hook));
    }

    /// <summary>
    /// Add an afterEach hook to a context with validation (async version).
    /// </summary>
    public static void AddAfterEach(SpecContext? context, Func<Task> hook)
    {
        EnsureContext(context);
        context!.AddAfterEach(hook);
    }

    /// <summary>
    /// Add a beforeAll hook to a context with validation (sync version).
    /// </summary>
    public static void AddBeforeAll(SpecContext? context, Action hook)
    {
        EnsureContext(context);
        context!.AddBeforeAll(WrapSync(hook));
    }

    /// <summary>
    /// Add a beforeAll hook to a context with validation (async version).
    /// </summary>
    public static void AddBeforeAll(SpecContext? context, Func<Task> hook)
    {
        EnsureContext(context);
        context!.AddBeforeAll(hook);
    }

    /// <summary>
    /// Add an afterAll hook to a context with validation (sync version).
    /// </summary>
    public static void AddAfterAll(SpecContext? context, Action hook)
    {
        EnsureContext(context);
        context!.AddAfterAll(WrapSync(hook));
    }

    /// <summary>
    /// Add an afterAll hook to a context with validation (async version).
    /// </summary>
    public static void AddAfterAll(SpecContext? context, Func<Task> hook)
    {
        EnsureContext(context);
        context!.AddAfterAll(hook);
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

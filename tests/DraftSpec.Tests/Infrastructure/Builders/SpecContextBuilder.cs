using DraftSpec.Middleware;

namespace DraftSpec.Tests.Infrastructure.Builders;

/// <summary>
/// A fluent builder for creating <see cref="SpecContext"/>, <see cref="SpecDefinition"/>,
/// and <see cref="SpecExecutionContext"/> instances for testing purposes.
/// </summary>
/// <remarks>
/// <para>
/// This builder reduces boilerplate in tests by providing:
/// </para>
/// <list type="bullet">
/// <item><description>Static factory methods for common scenarios</description></item>
/// <item><description>Fluent API for complex configurations</description></item>
/// <item><description>Consistent default values across all tests</description></item>
/// </list>
/// <para>
/// <b>Usage Examples:</b>
/// </para>
/// <code>
/// // Simple context
/// var context = SpecContextBuilder.CreateSimple("Calculator");
///
/// // Context with a spec
/// var context = SpecContextBuilder.WithSpec("adds numbers", () => { });
///
/// // Complex context via fluent builder
/// var context = new SpecContextBuilder()
///     .WithDescription("UserService")
///     .WithBeforeEach(() => SetupUser())
///     .WithSpec("creates user", () => { })
///     .WithSpec("deletes user", () => { })
///     .Build();
///
/// // Nested contexts
/// var context = new SpecContextBuilder()
///     .WithDescription("Calculator")
///     .WithChild(child => child
///         .WithDescription("add")
///         .WithSpec("returns sum", () => { }))
///     .Build();
///
/// // Execution context for middleware tests
/// var execContext = SpecContextBuilder.CreateExecutionContext(spec);
/// </code>
/// </remarks>
public class SpecContextBuilder
{
    private string _description = "test";
    private SpecContext? _parent;
    private int _lineNumber;
    private Func<Task>? _beforeAll;
    private Func<Task>? _afterAll;
    private Func<Task>? _beforeEach;
    private Func<Task>? _afterEach;
    private readonly List<SpecDefinition> _specs = [];
    private readonly List<Action<SpecContextBuilder>> _childBuilders = [];

    #region Static Factory Methods

    /// <summary>
    /// Creates a simple <see cref="SpecContext"/> with the specified description.
    /// </summary>
    /// <param name="description">The context description. Defaults to "test".</param>
    /// <returns>A new <see cref="SpecContext"/> instance.</returns>
    /// <example>
    /// <code>
    /// var context = SpecContextBuilder.CreateSimple("Calculator");
    /// </code>
    /// </example>
    public static SpecContext CreateSimple(string description = "test")
    {
        return new SpecContext(description);
    }

    /// <summary>
    /// Creates a <see cref="SpecContext"/> containing a single spec with a synchronous body.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The synchronous spec body.</param>
    /// <param name="contextDescription">The context description. Defaults to "test".</param>
    /// <returns>A new <see cref="SpecContext"/> containing the spec.</returns>
    /// <example>
    /// <code>
    /// var context = SpecContextBuilder.WithSpec("adds numbers", () =>
    /// {
    ///     expect(1 + 1).toBe(2);
    /// });
    /// </code>
    /// </example>
    public static SpecContext WithSpec(string description, Action body, string contextDescription = "test")
    {
        var context = new SpecContext(contextDescription);
        context.AddSpec(new SpecDefinition(description, body));
        return context;
    }

    /// <summary>
    /// Creates a <see cref="SpecContext"/> containing a single spec with an asynchronous body.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The asynchronous spec body.</param>
    /// <param name="contextDescription">The context description. Defaults to "test".</param>
    /// <returns>A new <see cref="SpecContext"/> containing the spec.</returns>
    /// <example>
    /// <code>
    /// var context = SpecContextBuilder.WithAsyncSpec("fetches data", async () =>
    /// {
    ///     var result = await service.FetchAsync();
    ///     expect(result).toNotBeNull();
    /// });
    /// </code>
    /// </example>
    public static SpecContext WithAsyncSpec(string description, Func<Task> body, string contextDescription = "test")
    {
        var context = new SpecContext(contextDescription);
        context.AddSpec(new SpecDefinition(description, body));
        return context;
    }

    /// <summary>
    /// Creates a <see cref="SpecContext"/> containing a pending spec (no body).
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="contextDescription">The context description. Defaults to "test".</param>
    /// <returns>A new <see cref="SpecContext"/> containing the pending spec.</returns>
    /// <example>
    /// <code>
    /// var context = SpecContextBuilder.WithPendingSpec("should handle edge case");
    /// // context.Specs[0].IsPending == true
    /// </code>
    /// </example>
    public static SpecContext WithPendingSpec(string description, string contextDescription = "test")
    {
        var context = new SpecContext(contextDescription);
        context.AddSpec(new SpecDefinition(description));
        return context;
    }

    /// <summary>
    /// Creates a <see cref="SpecContext"/> containing a skipped spec.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The spec body (will not be executed).</param>
    /// <param name="contextDescription">The context description. Defaults to "test".</param>
    /// <returns>A new <see cref="SpecContext"/> containing the skipped spec.</returns>
    /// <example>
    /// <code>
    /// var context = SpecContextBuilder.WithSkippedSpec("broken test", () => throw new Exception());
    /// // context.Specs[0].IsSkipped == true
    /// </code>
    /// </example>
    public static SpecContext WithSkippedSpec(string description, Action body, string contextDescription = "test")
    {
        var context = new SpecContext(contextDescription);
        context.AddSpec(new SpecDefinition(description, body) { IsSkipped = true });
        return context;
    }

    /// <summary>
    /// Creates a <see cref="SpecContext"/> containing a focused spec.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The spec body.</param>
    /// <param name="contextDescription">The context description. Defaults to "test".</param>
    /// <returns>A new <see cref="SpecContext"/> containing the focused spec.</returns>
    /// <example>
    /// <code>
    /// var context = SpecContextBuilder.WithFocusedSpec("specific test", () => { });
    /// // context.Specs[0].IsFocused == true
    /// // context.HasFocusedDescendants == true
    /// </code>
    /// </example>
    public static SpecContext WithFocusedSpec(string description, Action body, string contextDescription = "test")
    {
        var context = new SpecContext(contextDescription);
        context.AddSpec(new SpecDefinition(description, body) { IsFocused = true });
        return context;
    }

    /// <summary>
    /// Creates a <see cref="SpecContext"/> with a nested child context.
    /// </summary>
    /// <param name="parentDescription">The parent context description.</param>
    /// <param name="childDescription">The child context description.</param>
    /// <returns>A tuple containing the parent and child contexts.</returns>
    /// <example>
    /// <code>
    /// var (parent, child) = SpecContextBuilder.WithNestedContext("Calculator", "add");
    /// child.AddSpec(new SpecDefinition("returns sum", () => { }));
    /// </code>
    /// </example>
    public static (SpecContext Parent, SpecContext Child) WithNestedContext(
        string parentDescription = "parent",
        string childDescription = "child")
    {
        var parent = new SpecContext(parentDescription);
        var child = new SpecContext(childDescription, parent);
        return (parent, child);
    }

    /// <summary>
    /// Creates a <see cref="SpecExecutionContext"/> for middleware testing.
    /// </summary>
    /// <param name="spec">The spec definition to wrap.</param>
    /// <param name="contextDescription">The context description. Defaults to "test".</param>
    /// <param name="hasFocused">Whether focus mode is active. Defaults to false.</param>
    /// <returns>A new <see cref="SpecExecutionContext"/> instance.</returns>
    /// <example>
    /// <code>
    /// var spec = new SpecDefinition("test", () => { });
    /// var execContext = SpecContextBuilder.CreateExecutionContext(spec);
    ///
    /// var result = await middleware.ExecuteAsync(execContext, next);
    /// </code>
    /// </example>
    public static SpecExecutionContext CreateExecutionContext(
        SpecDefinition spec,
        string contextDescription = "test",
        bool hasFocused = false)
    {
        var specContext = new SpecContext(contextDescription);
        return new SpecExecutionContext
        {
            Spec = spec,
            Context = specContext,
            ContextPath = [contextDescription],
            HasFocused = hasFocused
        };
    }

    /// <summary>
    /// Creates a <see cref="SpecExecutionContext"/> with a simple passing spec.
    /// </summary>
    /// <param name="description">The spec description. Defaults to "test spec".</param>
    /// <param name="contextDescription">The context description. Defaults to "test".</param>
    /// <returns>A new <see cref="SpecExecutionContext"/> instance.</returns>
    /// <example>
    /// <code>
    /// var execContext = SpecContextBuilder.CreateSimpleExecutionContext();
    /// var result = await middleware.ExecuteAsync(execContext, next);
    /// </code>
    /// </example>
    public static SpecExecutionContext CreateSimpleExecutionContext(
        string description = "test spec",
        string contextDescription = "test")
    {
        var spec = new SpecDefinition(description, () => { });
        return CreateExecutionContext(spec, contextDescription);
    }

    /// <summary>
    /// Creates a <see cref="SpecExecutionContext"/> with a custom context path.
    /// </summary>
    /// <param name="spec">The spec definition.</param>
    /// <param name="contextPath">The context path segments.</param>
    /// <param name="hasFocused">Whether focus mode is active. Defaults to false.</param>
    /// <returns>A new <see cref="SpecExecutionContext"/> instance.</returns>
    /// <example>
    /// <code>
    /// var spec = new SpecDefinition("adds numbers", () => { });
    /// var execContext = SpecContextBuilder.CreateExecutionContextWithPath(
    ///     spec, ["Calculator", "math", "add"]);
    /// </code>
    /// </example>
    public static SpecExecutionContext CreateExecutionContextWithPath(
        SpecDefinition spec,
        IReadOnlyList<string> contextPath,
        bool hasFocused = false)
    {
        var contextDescription = contextPath.Count > 0 ? contextPath[^1] : "test";
        var specContext = new SpecContext(contextDescription);
        return new SpecExecutionContext
        {
            Spec = spec,
            Context = specContext,
            ContextPath = contextPath,
            HasFocused = hasFocused
        };
    }

    #endregion

    #region Fluent Builder Methods

    /// <summary>
    /// Sets the description for the context being built.
    /// </summary>
    /// <param name="description">The context description.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the parent context, making this a nested context.
    /// </summary>
    /// <param name="parent">The parent context.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithParent(SpecContext parent)
    {
        _parent = parent;
        return this;
    }

    /// <summary>
    /// Sets the line number for IDE navigation support.
    /// </summary>
    /// <param name="lineNumber">The line number in the source file.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithLineNumber(int lineNumber)
    {
        _lineNumber = lineNumber;
        return this;
    }

    /// <summary>
    /// Sets the beforeAll hook for the context.
    /// </summary>
    /// <param name="hook">The async hook to run once before all specs.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithBeforeAll(Func<Task> hook)
    {
        _beforeAll = hook;
        return this;
    }

    /// <summary>
    /// Sets the beforeAll hook for the context using a synchronous action.
    /// </summary>
    /// <param name="hook">The sync hook to run once before all specs.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithBeforeAll(Action hook)
    {
        _beforeAll = () =>
        {
            hook();
            return Task.CompletedTask;
        };
        return this;
    }

    /// <summary>
    /// Sets the afterAll hook for the context.
    /// </summary>
    /// <param name="hook">The async hook to run once after all specs.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithAfterAll(Func<Task> hook)
    {
        _afterAll = hook;
        return this;
    }

    /// <summary>
    /// Sets the afterAll hook for the context using a synchronous action.
    /// </summary>
    /// <param name="hook">The sync hook to run once after all specs.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithAfterAll(Action hook)
    {
        _afterAll = () =>
        {
            hook();
            return Task.CompletedTask;
        };
        return this;
    }

    /// <summary>
    /// Sets the beforeEach hook for the context.
    /// </summary>
    /// <param name="hook">The async hook to run before each spec.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithBeforeEach(Func<Task> hook)
    {
        _beforeEach = hook;
        return this;
    }

    /// <summary>
    /// Sets the beforeEach hook for the context using a synchronous action.
    /// </summary>
    /// <param name="hook">The sync hook to run before each spec.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithBeforeEach(Action hook)
    {
        _beforeEach = () =>
        {
            hook();
            return Task.CompletedTask;
        };
        return this;
    }

    /// <summary>
    /// Sets the afterEach hook for the context.
    /// </summary>
    /// <param name="hook">The async hook to run after each spec.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithAfterEach(Func<Task> hook)
    {
        _afterEach = hook;
        return this;
    }

    /// <summary>
    /// Sets the afterEach hook for the context using a synchronous action.
    /// </summary>
    /// <param name="hook">The sync hook to run after each spec.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithAfterEach(Action hook)
    {
        _afterEach = () =>
        {
            hook();
            return Task.CompletedTask;
        };
        return this;
    }

    /// <summary>
    /// Adds a spec with a synchronous body to the context.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The synchronous spec body.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithSpec(string description, Action body)
    {
        _specs.Add(new SpecDefinition(description, body));
        return this;
    }

    /// <summary>
    /// Adds a spec with an asynchronous body to the context.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The asynchronous spec body.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithSpec(string description, Func<Task> body)
    {
        _specs.Add(new SpecDefinition(description, body));
        return this;
    }

    /// <summary>
    /// Adds a pending spec (no body) to the context.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithPendingSpec(string description)
    {
        _specs.Add(new SpecDefinition(description));
        return this;
    }

    /// <summary>
    /// Adds a skipped spec to the context.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The spec body (will not be executed).</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithSkippedSpec(string description, Action body)
    {
        _specs.Add(new SpecDefinition(description, body) { IsSkipped = true });
        return this;
    }

    /// <summary>
    /// Adds a focused spec to the context.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The spec body.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithFocusedSpec(string description, Action body)
    {
        _specs.Add(new SpecDefinition(description, body) { IsFocused = true });
        return this;
    }

    /// <summary>
    /// Adds a tagged spec to the context.
    /// </summary>
    /// <param name="description">The spec description.</param>
    /// <param name="body">The spec body.</param>
    /// <param name="tags">The tags to associate with the spec.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithTaggedSpec(string description, Action body, params string[] tags)
    {
        _specs.Add(new SpecDefinition(description, body) { Tags = tags });
        return this;
    }

    /// <summary>
    /// Adds a pre-built spec definition to the context.
    /// </summary>
    /// <param name="spec">The spec definition to add.</param>
    /// <returns>The builder for method chaining.</returns>
    public SpecContextBuilder WithSpec(SpecDefinition spec)
    {
        _specs.Add(spec);
        return this;
    }

    /// <summary>
    /// Adds a nested child context using a builder callback.
    /// </summary>
    /// <param name="childBuilder">A callback that configures the child context builder.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var context = new SpecContextBuilder()
    ///     .WithDescription("Calculator")
    ///     .WithChild(child => child
    ///         .WithDescription("add")
    ///         .WithSpec("returns sum", () => { }))
    ///     .Build();
    /// </code>
    /// </example>
    public SpecContextBuilder WithChild(Action<SpecContextBuilder> childBuilder)
    {
        _childBuilders.Add(childBuilder);
        return this;
    }

    /// <summary>
    /// Builds the configured <see cref="SpecContext"/>.
    /// </summary>
    /// <returns>The configured <see cref="SpecContext"/> instance.</returns>
    public SpecContext Build()
    {
        var context = new SpecContext(_description, _parent)
        {
            LineNumber = _lineNumber
        };

        // Set hooks
        context.BeforeAll = _beforeAll;
        context.AfterAll = _afterAll;
        context.BeforeEach = _beforeEach;
        context.AfterEach = _afterEach;

        // Add specs
        foreach (var spec in _specs)
        {
            context.AddSpec(spec);
        }

        // Build and attach children
        foreach (var childBuilderAction in _childBuilders)
        {
            var childBuilder = new SpecContextBuilder().WithParent(context);
            childBuilderAction(childBuilder);
            childBuilder.Build(); // Building with parent auto-attaches
        }

        return context;
    }

    #endregion
}

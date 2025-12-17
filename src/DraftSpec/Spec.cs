using DraftSpec.Internal;

namespace DraftSpec;

/// <summary>
/// Base class for class-based spec definitions. Inherit from this to define specs.
/// </summary>
/// <remarks>
/// This is an alternative to the static <see cref="Dsl"/> API. It provides instance methods
/// for defining specs, which may be preferred in traditional test projects.
/// </remarks>
/// <example>
/// <code>
/// public class CalculatorSpecs : Spec
/// {
///     public override void Define()
///     {
///         describe("Calculator", () =>
///         {
///             it("adds numbers", () => expect(1 + 1).toBe(2));
///         });
///     }
/// }
/// </code>
/// </example>
public abstract class Spec
{
    /// <summary>
    /// The currently active context during spec definition.
    /// Instance field provides isolation between Spec instances.
    /// </summary>
    private SpecContext? _currentContext;

    /// <summary>
    /// The root context for this spec class, named after the class type.
    /// </summary>
    public SpecContext RootContext { get; }

    /// <summary>
    /// The currently active context during spec definition.
    /// </summary>
    protected SpecContext? CurrentContext
    {
        get => _currentContext;
        private set => _currentContext = value;
    }

    /// <summary>
    /// Initializes the spec with a root context named after the class.
    /// </summary>
    protected Spec()
    {
        RootContext = new SpecContext(GetType().Name);
        _currentContext = RootContext;
    }

    /// <summary>
    /// Defines a context block for grouping related specs.
    /// </summary>
    /// <param name="description">Description of what this context covers.</param>
    /// <param name="body">Action that defines the specs and nested contexts.</param>
    protected void describe(string description, Action body)
    {
        var parent = CurrentContext!;
        var context = ContextBuilder.CreateNestedContext(description, parent);
        CurrentContext = context;
        try
        {
            body();
        }
        finally
        {
            CurrentContext = parent;
        }
    }

    /// <summary>
    /// Alias for <see cref="describe"/> - used for sub-groupings within a describe block.
    /// </summary>
    /// <param name="description">Description of the sub-context.</param>
    /// <param name="body">Action that defines the specs and nested contexts.</param>
    protected void context(string description, Action body)
    {
        describe(description, body);
    }

    /// <summary>
    /// Defines a spec with an implementation.
    /// </summary>
    /// <param name="description">What the spec tests.</param>
    /// <param name="body">The test implementation.</param>
    protected void it(string description, Action body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSpec(description, body));
    }

    /// <summary>
    /// Defines a pending spec (no implementation yet).
    /// </summary>
    /// <param name="description">What the spec will test.</param>
    protected void it(string description)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreatePendingSpec(description));
    }

    /// <summary>
    /// Defines a focused spec. When any focused specs exist, only focused specs run.
    /// </summary>
    /// <param name="description">What the spec tests.</param>
    /// <param name="body">The test implementation.</param>
    protected void fit(string description, Action body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateFocusedSpec(description, body));
    }

    /// <summary>
    /// Defines a skipped spec that will not be executed.
    /// </summary>
    /// <param name="description">What the spec would test.</param>
    /// <param name="body">Optional body (will not be executed).</param>
    protected void xit(string description, Action? body = null)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSkippedSpec(description, body));
    }

    /// <summary>
    /// Sets a hook that runs once before any spec in the current context.
    /// </summary>
    protected Action beforeAll
    {
        set => ContextBuilder.SetBeforeAll(CurrentContext, value);
    }

    /// <summary>
    /// Sets a hook that runs once after all specs in the current context.
    /// </summary>
    protected Action afterAll
    {
        set => ContextBuilder.SetAfterAll(CurrentContext, value);
    }

    /// <summary>
    /// Sets a hook that runs before each spec in the current context.
    /// </summary>
    protected Action before
    {
        set => ContextBuilder.SetBeforeEach(CurrentContext, value);
    }

    /// <summary>
    /// Sets a hook that runs after each spec in the current context.
    /// </summary>
    protected Action after
    {
        set => ContextBuilder.SetAfterEach(CurrentContext, value);
    }
}
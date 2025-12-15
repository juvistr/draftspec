using DraftSpec.Internal;

namespace DraftSpec;

/// <summary>
/// Base class for spec definitions. Inherit from this to define specs.
/// </summary>
public abstract class Spec
{
    private static readonly AsyncLocal<SpecContext?> CurrentContextLocal = new();

    public SpecContext RootContext { get; }

    protected static SpecContext? CurrentContext
    {
        get => CurrentContextLocal.Value;
        private set => CurrentContextLocal.Value = value;
    }

    protected Spec()
    {
        RootContext = new SpecContext(GetType().Name);
        CurrentContext = RootContext;
    }

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

    // Alias for describe - used for sub-groupings
    protected void context(string description, Action body) => describe(description, body);

    // Spec with implementation
    protected void it(string description, Action body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSpec(description, body));
    }

    // Pending spec - no implementation yet
    protected void it(string description)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreatePendingSpec(description));
    }

    // Focused spec - only run focused specs when any exist
    protected void fit(string description, Action body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateFocusedSpec(description, body));
    }

    // Skipped spec
    protected void xit(string description, Action? body = null)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSkippedSpec(description, body));
    }

    // Hook setters - use ContextBuilder for validation
    protected Action beforeAll
    {
        set => ContextBuilder.SetBeforeAll(CurrentContext, value);
    }

    protected Action afterAll
    {
        set => ContextBuilder.SetAfterAll(CurrentContext, value);
    }

    protected Action before
    {
        set => ContextBuilder.SetBeforeEach(CurrentContext, value);
    }

    protected Action after
    {
        set => ContextBuilder.SetAfterEach(CurrentContext, value);
    }
}

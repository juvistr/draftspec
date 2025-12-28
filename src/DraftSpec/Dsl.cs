namespace DraftSpec;

/// <summary>
/// Static DSL for script-based specs (.csx files).
/// Usage: using static DraftSpec.Dsl;
/// </summary>
/// <remarks>
/// This partial class contains the session accessor and core state properties.
/// See also: Dsl.Context.cs, Dsl.Specs.cs, Dsl.Hooks.cs, Dsl.Expect.cs, Dsl.Run.cs
/// </remarks>
public static partial class Dsl
{
    private static readonly AsyncLocal<SpecSession?> SessionLocal = new();

    /// <summary>
    /// Gets the current spec session for this async execution context.
    /// A new session is created automatically if one doesn't exist.
    /// </summary>
    public static SpecSession Session => SessionLocal.Value ??= new SpecSession();

    internal static SpecContext? CurrentContext
    {
        get => Session.CurrentContext;
        set => Session.CurrentContext = value;
    }

    /// <summary>
    /// Gets the root context containing the spec tree after describe() blocks are executed.
    /// Used by MTP integration to access the spec tree for discovery and execution.
    /// </summary>
    public static SpecContext? RootContext
    {
        get => Session.RootContext;
        internal set => Session.RootContext = value;
    }
}

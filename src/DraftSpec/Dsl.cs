namespace DraftSpec;

/// <summary>
/// Static DSL for script-based specs (.csx files).
/// Usage: using static DraftSpec.Dsl;
/// </summary>
/// <remarks>
/// This partial class contains state management.
/// See also: Dsl.Context.cs, Dsl.Specs.cs, Dsl.Hooks.cs, Dsl.Expect.cs, Dsl.Run.cs
/// </remarks>
public static partial class Dsl
{
    private static readonly AsyncLocal<SpecContext?> CurrentContextLocal = new();
    private static readonly AsyncLocal<SpecContext?> RootContextLocal = new();

    internal static SpecContext? CurrentContext
    {
        get => CurrentContextLocal.Value;
        set => CurrentContextLocal.Value = value;
    }

    /// <summary>
    /// Gets the root context containing the spec tree after describe() blocks are executed.
    /// Used by MTP integration to access the spec tree for discovery and execution.
    /// </summary>
    public static SpecContext? RootContext
    {
        get => RootContextLocal.Value;
        internal set => RootContextLocal.Value = value;
    }
}
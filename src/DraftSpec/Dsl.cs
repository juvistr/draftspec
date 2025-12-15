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

    internal static SpecContext? RootContext
    {
        get => RootContextLocal.Value;
        set => RootContextLocal.Value = value;
    }
}

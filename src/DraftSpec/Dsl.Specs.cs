using DraftSpec.Internal;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Define a spec with implementation.
    /// </summary>
    public static void it(string description, Action body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSpec(description, body));
    }

    /// <summary>
    /// Define a pending spec (no implementation yet).
    /// </summary>
    public static void it(string description)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreatePendingSpec(description));
    }

    /// <summary>
    /// Define a focused spec - only focused specs run when any exist.
    /// </summary>
    public static void fit(string description, Action body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateFocusedSpec(description, body));
    }

    /// <summary>
    /// Define a skipped spec.
    /// </summary>
    public static void xit(string description, Action? body = null)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSkippedSpec(description, body));
    }
}

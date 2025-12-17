using DraftSpec.Internal;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Define a spec with a sync implementation.
    /// </summary>
    public static void it(string description, Action body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSpec(description, body, CurrentTags));
    }

    /// <summary>
    /// Define a spec with an async implementation.
    /// </summary>
    public static void it(string description, Func<Task> body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSpec(description, body, CurrentTags));
    }

    /// <summary>
    /// Define a pending spec (no implementation yet).
    /// </summary>
    public static void it(string description)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreatePendingSpec(description, CurrentTags));
    }

    /// <summary>
    /// Define a focused spec with a sync implementation - only focused specs run when any exist.
    /// </summary>
    public static void fit(string description, Action body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateFocusedSpec(description, body, CurrentTags));
    }

    /// <summary>
    /// Define a focused spec with an async implementation - only focused specs run when any exist.
    /// </summary>
    public static void fit(string description, Func<Task> body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateFocusedSpec(description, body, CurrentTags));
    }

    /// <summary>
    /// Define a skipped spec with a sync body.
    /// </summary>
    public static void xit(string description, Action? body = null)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSkippedSpec(description, body, CurrentTags));
    }

    /// <summary>
    /// Define a skipped spec with an async body.
    /// </summary>
    public static void xit(string description, Func<Task>? body)
    {
        ContextBuilder.AddSpec(CurrentContext, ContextBuilder.CreateSkippedSpec(description, body, CurrentTags));
    }
}
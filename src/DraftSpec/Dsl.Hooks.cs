using DraftSpec.Internal;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Register a sync beforeEach hook for the current context.
    /// </summary>
    public static void before(Action hook)
    {
        ContextBuilder.SetBeforeEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register an async beforeEach hook for the current context.
    /// </summary>
    public static void before(Func<Task> hook)
    {
        ContextBuilder.SetBeforeEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register a sync afterEach hook for the current context.
    /// </summary>
    public static void after(Action hook)
    {
        ContextBuilder.SetAfterEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register an async afterEach hook for the current context.
    /// </summary>
    public static void after(Func<Task> hook)
    {
        ContextBuilder.SetAfterEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register a sync beforeAll hook for the current context.
    /// </summary>
    public static void beforeAll(Action hook)
    {
        ContextBuilder.SetBeforeAll(CurrentContext, hook);
    }

    /// <summary>
    /// Register an async beforeAll hook for the current context.
    /// </summary>
    public static void beforeAll(Func<Task> hook)
    {
        ContextBuilder.SetBeforeAll(CurrentContext, hook);
    }

    /// <summary>
    /// Register a sync afterAll hook for the current context.
    /// </summary>
    public static void afterAll(Action hook)
    {
        ContextBuilder.SetAfterAll(CurrentContext, hook);
    }

    /// <summary>
    /// Register an async afterAll hook for the current context.
    /// </summary>
    public static void afterAll(Func<Task> hook)
    {
        ContextBuilder.SetAfterAll(CurrentContext, hook);
    }
}

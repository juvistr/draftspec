using DraftSpec.Internal;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Register a sync beforeEach hook for the current context.
    /// </summary>
    public static void before(Action hook)
    {
        ContextBuilder.AddBeforeEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register an async beforeEach hook for the current context.
    /// </summary>
    public static void before(Func<Task> hook)
    {
        ContextBuilder.AddBeforeEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register a sync afterEach hook for the current context.
    /// </summary>
    public static void after(Action hook)
    {
        ContextBuilder.AddAfterEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register an async afterEach hook for the current context.
    /// </summary>
    public static void after(Func<Task> hook)
    {
        ContextBuilder.AddAfterEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register a sync beforeAll hook for the current context.
    /// </summary>
    public static void beforeAll(Action hook)
    {
        ContextBuilder.AddBeforeAll(CurrentContext, hook);
    }

    /// <summary>
    /// Register an async beforeAll hook for the current context.
    /// </summary>
    public static void beforeAll(Func<Task> hook)
    {
        ContextBuilder.AddBeforeAll(CurrentContext, hook);
    }

    /// <summary>
    /// Register a sync afterAll hook for the current context.
    /// </summary>
    public static void afterAll(Action hook)
    {
        ContextBuilder.AddAfterAll(CurrentContext, hook);
    }

    /// <summary>
    /// Register an async afterAll hook for the current context.
    /// </summary>
    public static void afterAll(Func<Task> hook)
    {
        ContextBuilder.AddAfterAll(CurrentContext, hook);
    }
}

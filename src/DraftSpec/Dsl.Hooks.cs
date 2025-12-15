using DraftSpec.Internal;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Register a beforeEach hook for the current context.
    /// </summary>
    public static void before(Action hook)
    {
        ContextBuilder.SetBeforeEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register an afterEach hook for the current context.
    /// </summary>
    public static void after(Action hook)
    {
        ContextBuilder.SetAfterEach(CurrentContext, hook);
    }

    /// <summary>
    /// Register a beforeAll hook for the current context.
    /// </summary>
    public static void beforeAll(Action hook)
    {
        ContextBuilder.SetBeforeAll(CurrentContext, hook);
    }

    /// <summary>
    /// Register an afterAll hook for the current context.
    /// </summary>
    public static void afterAll(Action hook)
    {
        ContextBuilder.SetAfterAll(CurrentContext, hook);
    }
}

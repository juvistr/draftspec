using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Infrastructure;

/// <summary>
/// Base class for tests that use the DraftSpec DSL (describe, it, expect, etc.).
/// Automatically resets global DSL state before each test.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// public class MyDslTests : DslTestBase
/// {
///     [Test]
///     public async Task MyTest()
///     {
///         describe("feature", () =>
///         {
///             it("works", () => { });
///         });
///
///         await new SpecRunner().RunAsync(RootContext!);
///     }
/// }
/// </code>
/// </remarks>
public abstract class DslTestBase
{
    /// <summary>
    /// Gets the root context containing the spec tree after describe() blocks are executed.
    /// This is a convenience accessor for <see cref="DraftSpec.Dsl.RootContext"/>.
    /// </summary>
    protected static SpecContext? RootContext => DraftSpec.Dsl.RootContext;

    /// <summary>
    /// Resets the DSL state before each test to ensure a clean execution context.
    /// This clears any previously defined specs, contexts, and hooks.
    /// </summary>
    [Before(Test)]
    public void ResetDslState() => Reset();
}

namespace DraftSpec;

/// <summary>
/// Expectation wrapper for boolean values with boolean-specific assertions.
/// </summary>
/// <remarks>
/// Created via <c>expect(boolValue)</c>. Provides assertions like <c>toBeTrue()</c>
/// and <c>toBeFalse()</c>.
/// Extension methods can access <see cref="Actual"/> and <see cref="Expression"/>
/// to create custom matchers.
/// </remarks>
public class BoolExpectation
{
    /// <summary>
    /// The actual boolean value being asserted.
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public bool Actual { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates an expectation for the specified boolean value.
    /// </summary>
    /// <param name="actual">The actual boolean value to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public BoolExpectation(bool actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the value is true.
    /// </summary>
    public void toBeTrue()
    {
        if (!Actual)
            throw new AssertionException(
                $"Expected {Expression} to be true, but was false");
    }

    /// <summary>
    /// Assert that the value is false.
    /// </summary>
    public void toBeFalse()
    {
        if (Actual)
            throw new AssertionException(
                $"Expected {Expression} to be false, but was true");
    }

    /// <summary>
    /// Assert equality (for consistency with Expectation&lt;T&gt;).
    /// </summary>
    public void toBe(bool expected)
    {
        if (Actual != expected)
            throw new AssertionException(
                $"Expected {Expression} to be {expected}, but was {Actual}");
    }
}

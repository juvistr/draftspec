namespace DraftSpec;

/// <summary>
/// Expectation wrapper for boolean values with boolean-specific assertions.
/// </summary>
/// <remarks>
/// Created via <c>expect(boolValue)</c>. Provides assertions like <c>toBeTrue()</c>
/// and <c>toBeFalse()</c>.
/// </remarks>
public class BoolExpectation
{
    private readonly bool _actual;
    private readonly string? _expr;

    /// <summary>
    /// Creates an expectation for the specified boolean value.
    /// </summary>
    /// <param name="actual">The actual boolean value to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public BoolExpectation(bool actual, string? expr)
    {
        _actual = actual;
        _expr = expr;
    }

    /// <summary>
    /// Assert that the value is true.
    /// </summary>
    public void toBeTrue()
    {
        if (!_actual)
            throw new AssertionException(
                $"Expected {_expr} to be true, but was false");
    }

    /// <summary>
    /// Assert that the value is false.
    /// </summary>
    public void toBeFalse()
    {
        if (_actual)
            throw new AssertionException(
                $"Expected {_expr} to be false, but was true");
    }

    /// <summary>
    /// Assert equality (for consistency with Expectation&lt;T&gt;).
    /// </summary>
    public void toBe(bool expected)
    {
        if (_actual != expected)
            throw new AssertionException(
                $"Expected {_expr} to be {expected}, but was {_actual}");
    }
}

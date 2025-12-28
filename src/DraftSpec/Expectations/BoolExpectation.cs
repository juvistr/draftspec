namespace DraftSpec;

/// <summary>
/// Expectation wrapper for boolean values with boolean-specific assertions.
/// Supports both positive and negated assertions via the <see cref="not"/> property.
/// </summary>
/// <remarks>
/// Created via <c>expect(boolValue)</c>. Provides assertions like <c>toBeTrue()</c>
/// and <c>toBeFalse()</c>.
/// Extension methods can access <see cref="Actual"/> and <see cref="Expression"/>
/// to create custom matchers.
/// </remarks>
public readonly struct BoolExpectation
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

    private readonly bool _isNegated;

    /// <summary>
    /// Creates an expectation for the specified boolean value.
    /// </summary>
    /// <param name="actual">The actual boolean value to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public BoolExpectation(bool actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
        _isNegated = false;
    }

    /// <summary>
    /// Creates an expectation for the specified boolean value with negation control.
    /// </summary>
    internal BoolExpectation(bool actual, string? expr, bool isNegated)
    {
        Actual = actual;
        Expression = expr;
        _isNegated = isNegated;
    }

    /// <summary>
    /// Returns a negated expectation for chaining negative assertions.
    /// </summary>
    /// <example>
    /// <code>
    /// expect(value).not.toBeTrue();
    /// expect(value).not.toBeFalse();
    /// </code>
    /// </example>
    public BoolExpectation not => new(Actual, Expression, !_isNegated);

    /// <summary>
    /// Assert that the value is (or is not, if negated) true.
    /// </summary>
    public void toBeTrue()
    {
        if (_isNegated)
        {
            if (Actual)
                throw new AssertionException(
                    $"Expected {Expression} to not be true, but was true");
        }
        else
        {
            if (!Actual)
                throw new AssertionException(
                    $"Expected {Expression} to be true, but was false");
        }
    }

    /// <summary>
    /// Assert that the value is (or is not, if negated) false.
    /// </summary>
    public void toBeFalse()
    {
        if (_isNegated)
        {
            if (!Actual)
                throw new AssertionException(
                    $"Expected {Expression} to not be false, but was false");
        }
        else
        {
            if (Actual)
                throw new AssertionException(
                    $"Expected {Expression} to be false, but was true");
        }
    }

    /// <summary>
    /// Assert equality (or inequality, if negated).
    /// </summary>
    public void toBe(bool expected)
    {
        if (_isNegated)
        {
            if (Actual == expected)
                throw new AssertionException(
                    $"Expected {Expression} to not be {expected}");
        }
        else
        {
            if (Actual != expected)
                throw new AssertionException(
                    $"Expected {Expression} to be {expected}, but was {Actual}");
        }
    }
}

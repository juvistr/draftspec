namespace DraftSpec;

/// <summary>
/// Negated expectation wrapper for boolean assertions.
/// Returned by <c>expect(bool).not</c> to enable negative assertions.
/// </summary>
/// <example>
/// <code>
/// expect(value).not.toBeTrue();
/// expect(value).not.toBeFalse();
/// </code>
/// </example>
public readonly struct NegatedBoolExpectation
{
    /// <summary>
    /// The actual boolean value being asserted.
    /// </summary>
    public bool Actual { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates a negated expectation for the specified boolean value.
    /// </summary>
    public NegatedBoolExpectation(bool actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the value is NOT true (i.e., it is false).
    /// </summary>
    public void toBeTrue()
    {
        if (Actual)
            throw new AssertionException(
                $"Expected {Expression} to not be true, but was true");
    }

    /// <summary>
    /// Assert that the value is NOT false (i.e., it is true).
    /// </summary>
    public void toBeFalse()
    {
        if (!Actual)
            throw new AssertionException(
                $"Expected {Expression} to not be false, but was false");
    }

    /// <summary>
    /// Assert that the value does NOT equal the expected value.
    /// </summary>
    public void toBe(bool expected)
    {
        if (Actual == expected)
            throw new AssertionException(
                $"Expected {Expression} to not be {expected}");
    }
}

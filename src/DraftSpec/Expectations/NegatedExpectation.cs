using DraftSpec.Expectations;

namespace DraftSpec;

/// <summary>
/// Negated expectation wrapper for fluent assertions.
/// Returned by <c>expect(value).not</c> to enable negative assertions.
/// </summary>
/// <typeparam name="T">The type of value being asserted.</typeparam>
/// <example>
/// <code>
/// expect(value).not.toBe(5);
/// expect(value).not.toBeNull();
/// </code>
/// </example>
public readonly struct NegatedExpectation<T>
{
    /// <summary>
    /// The actual value being asserted.
    /// </summary>
    public T Actual { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates a negated expectation for the specified value.
    /// </summary>
    public NegatedExpectation(T actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the actual value does NOT equal the expected value.
    /// </summary>
    public void toBe(T expected)
    {
        if (Equals(Actual, expected))
            throw new AssertionException(
                $"Expected {Expression} to not be {ExpectationHelpers.Format(expected)}");
    }

    /// <summary>
    /// Assert that the actual value is NOT null.
    /// </summary>
    public void toBeNull()
    {
        if (Actual is null)
            throw new AssertionException(
                $"Expected {Expression} to not be null");
    }

    // Cached comparer to avoid repeated lookups
    private static readonly Comparer<T> _comparer = Comparer<T>.Default;

    /// <summary>
    /// Assert that the actual value is NOT greater than the expected value.
    /// </summary>
    public void toBeGreaterThan(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        if (_comparer.Compare(Actual, expected) > 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to not be greater than {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is NOT less than the expected value.
    /// </summary>
    public void toBeLessThan(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        if (_comparer.Compare(Actual, expected) < 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to not be less than {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is NOT in the specified range (inclusive).
    /// </summary>
    public void toBeInRange(T min, T max)
    {
        if (min is null || max is null)
            throw new AssertionException("Range bounds cannot be null for comparison");

        if (_comparer.Compare(Actual, min) >= 0 && _comparer.Compare(Actual, max) <= 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to not be in range [{ExpectationHelpers.Format(min)}, {ExpectationHelpers.Format(max)}], but was {ExpectationHelpers.Format(Actual)}");
    }
}

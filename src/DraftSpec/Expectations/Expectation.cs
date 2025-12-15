using DraftSpec.Expectations;

namespace DraftSpec;

/// <summary>
/// Expectation wrapper for fluent assertions on values of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of value being asserted.</typeparam>
/// <remarks>
/// Created via <c>expect(value)</c>. Provides assertions like <c>toBe()</c>,
/// <c>toBeNull()</c>, <c>toBeGreaterThan()</c>, etc.
/// Extension methods can access <see cref="Actual"/> and <see cref="Expression"/>
/// to create custom matchers.
/// </remarks>
/// <example>
/// Custom matcher via extension method:
/// <code>
/// public static class DateExpectationExtensions
/// {
///     public static void toBeAfter(this Expectation&lt;DateTime&gt; exp, DateTime other)
///     {
///         if (exp.Actual &lt;= other)
///             throw new AssertionException(
///                 $"Expected {exp.Expression} to be after {other}, but was {exp.Actual}");
///     }
/// }
/// </code>
/// </example>
public class Expectation<T>
{
    /// <summary>
    /// The actual value being asserted.
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public T Actual { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates an expectation for the specified value.
    /// </summary>
    /// <param name="actual">The actual value to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public Expectation(T actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
    }

    /// <summary>
    /// Asserts that the actual value equals the expected value.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <exception cref="AssertionException">Thrown when values are not equal.</exception>
    public void toBe(T expected)
    {
        if (!Equals(Actual, expected))
            throw new AssertionException(
                $"Expected {Expression} to be {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is null.
    /// </summary>
    public void toBeNull()
    {
        if (Actual is not null)
            throw new AssertionException(
                $"Expected {Expression} to be null, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is not null.
    /// </summary>
    public void toNotBeNull()
    {
        if (Actual is null)
            throw new AssertionException(
                $"Expected {Expression} to not be null");
    }

    /// <summary>
    /// Assert that the actual value is greater than the expected value.
    /// </summary>
    public void toBeGreaterThan(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        if (Actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(expected) <= 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to be greater than {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is less than the expected value.
    /// </summary>
    public void toBeLessThan(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        if (Actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(expected) >= 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to be less than {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is greater than or equal to the expected value.
    /// </summary>
    public void toBeAtLeast(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        if (Actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(expected) < 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to be at least {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is less than or equal to the expected value.
    /// </summary>
    public void toBeAtMost(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        if (Actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(expected) > 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to be at most {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is within the specified range (inclusive).
    /// </summary>
    public void toBeInRange(T min, T max)
    {
        if (min is null || max is null)
            throw new AssertionException("Range bounds cannot be null for comparison");

        if (Actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(min) < 0 || comparable.CompareTo(max) > 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to be in range [{ExpectationHelpers.Format(min)}, {ExpectationHelpers.Format(max)}], but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is close to the expected value within a tolerance.
    /// Works with numeric types (int, double, decimal, etc.).
    /// </summary>
    public void toBeCloseTo(T expected, T tolerance)
    {
        var diff = GetNumericDifference(Actual, expected);
        var tol = ConvertToDouble(tolerance);

        if (diff is null || tol is null)
            throw new AssertionException(
                $"toBeCloseTo requires numeric types, but got {typeof(T).Name}");

        if (diff.Value > tol.Value)
            throw new AssertionException(
                $"Expected {Expression} to be close to {ExpectationHelpers.Format(expected)} (±{ExpectationHelpers.Format(tolerance)}), but was {ExpectationHelpers.Format(Actual)} (diff: {diff.Value})");
    }

    private static double? GetNumericDifference(T? a, T? b)
    {
        var aVal = ConvertToDouble(a);
        var bVal = ConvertToDouble(b);
        if (aVal is null || bVal is null) return null;
        return Math.Abs(aVal.Value - bVal.Value);
    }

    private static double? ConvertToDouble(T? value)
    {
        return value switch
        {
            double d => d,
            float f => f,
            decimal m => (double)m,
            int i => i,
            long l => l,
            short s => s,
            byte by => by,
            uint ui => ui,
            ulong ul => ul,
            ushort us => us,
            sbyte sb => sb,
            _ => null
        };
    }

}

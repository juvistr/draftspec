using DraftSpec.Expectations;

namespace DraftSpec;

/// <summary>
/// Expectation wrapper for fluent assertions on values of type <typeparamref name="T"/>.
/// Supports both positive and negated assertions via the <see cref="not"/> property.
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
public readonly struct Expectation<T>
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

    private readonly bool _isNegated;

    /// <summary>
    /// Creates an expectation for the specified value.
    /// </summary>
    /// <param name="actual">The actual value to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public Expectation(T actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
        _isNegated = false;
    }

    /// <summary>
    /// Creates an expectation for the specified value with negation control.
    /// </summary>
    internal Expectation(T actual, string? expr, bool isNegated)
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
    /// expect(value).not.toBe(5);
    /// expect(value).not.toBeNull();
    /// </code>
    /// </example>
    public Expectation<T> not => new(Actual, Expression, !_isNegated);

    /// <summary>
    /// Asserts that the actual value equals (or does not equal, if negated) the expected value.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <exception cref="AssertionException">Thrown when assertion fails.</exception>
    public void toBe(T expected)
    {
        var equal = Equals(Actual, expected);
        if (_isNegated)
        {
            if (equal)
                throw new AssertionException(
                    $"Expected {Expression} to not be {ExpectationHelpers.Format(expected)}");
        }
        else
        {
            if (!equal)
                throw new AssertionException(
                    $"Expected {Expression} to be {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
        }
    }

    /// <summary>
    /// Assert that the actual value is null (or not null, if negated).
    /// </summary>
    public void toBeNull()
    {
        if (_isNegated)
        {
            if (Actual is null)
                throw new AssertionException(
                    $"Expected {Expression} to not be null");
        }
        else
        {
            if (Actual is not null)
                throw new AssertionException(
                    $"Expected {Expression} to be null, but was {ExpectationHelpers.Format(Actual)}");
        }
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
    /// Assert that the actual value is (or is not, if negated) an instance of the specified type.
    /// </summary>
    /// <typeparam name="TExpected">The expected type.</typeparam>
    /// <returns>The value cast to the expected type for further assertions (positive only).</returns>
    public TExpected toBeInstanceOf<TExpected>()
    {
        if (_isNegated)
        {
            if (Actual is TExpected)
                throw new AssertionException(
                    $"Expected {Expression} to not be instance of {typeof(TExpected).Name}, but it was");
            return default!;
        }

        if (Actual is null)
            throw new AssertionException(
                $"Expected {Expression} to be instance of {typeof(TExpected).Name}, but was null");

        if (Actual is not TExpected typedValue)
            throw new AssertionException(
                $"Expected {Expression} to be instance of {typeof(TExpected).Name}, but was {Actual.GetType().Name}");

        return typedValue;
    }

    /// <summary>
    /// Assert that the actual value is (or is not, if negated) equivalent to the expected value using deep comparison.
    /// Uses JSON serialization to compare object structures.
    /// </summary>
    /// <param name="expected">The expected value to compare against.</param>
    public void toBeEquivalentTo(T expected)
    {
        if (_isNegated)
        {
            if (Actual is null && expected is null)
                throw new AssertionException(
                    $"Expected {Expression} to not be equivalent to null, but both were null");

            if (Actual is null || expected is null)
                return; // One is null and the other isn't - they're not equivalent

            var actualJson = System.Text.Json.JsonSerializer.Serialize(Actual);
            var expectedJson = System.Text.Json.JsonSerializer.Serialize(expected);

            if (string.Equals(actualJson, expectedJson, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to not be equivalent to {ExpectationHelpers.Format(expected)}, but it was");
        }
        else
        {
            if (Actual is null && expected is null)
                return;

            if (Actual is null)
                throw new AssertionException(
                    $"Expected {Expression} to be equivalent to {ExpectationHelpers.Format(expected)}, but was null");

            if (expected is null)
                throw new AssertionException(
                    $"Expected {Expression} to be equivalent to null, but was {ExpectationHelpers.Format(Actual)}");

            var actualJson = System.Text.Json.JsonSerializer.Serialize(Actual);
            var expectedJson = System.Text.Json.JsonSerializer.Serialize(expected);

            if (!string.Equals(actualJson, expectedJson, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to be equivalent to {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
        }
    }

    // Cached comparer to avoid repeated lookups - uses optimized paths for primitives
    private static readonly Comparer<T> _comparer = Comparer<T>.Default;

    /// <summary>
    /// Assert that the actual value is (or is not, if negated) greater than the expected value.
    /// </summary>
    public void toBeGreaterThan(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        var isGreater = _comparer.Compare(Actual, expected) > 0;

        if (_isNegated)
        {
            if (isGreater)
                throw new AssertionException(
                    $"Expected {Expression ?? "value"} to not be greater than {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
        }
        else
        {
            if (!isGreater)
                throw new AssertionException(
                    $"Expected {Expression ?? "value"} to be greater than {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
        }
    }

    /// <summary>
    /// Assert that the actual value is (or is not, if negated) less than the expected value.
    /// </summary>
    public void toBeLessThan(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        var isLess = _comparer.Compare(Actual, expected) < 0;

        if (_isNegated)
        {
            if (isLess)
                throw new AssertionException(
                    $"Expected {Expression ?? "value"} to not be less than {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
        }
        else
        {
            if (!isLess)
                throw new AssertionException(
                    $"Expected {Expression ?? "value"} to be less than {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
        }
    }

    /// <summary>
    /// Assert that the actual value is greater than or equal to the expected value.
    /// </summary>
    public void toBeAtLeast(T expected)
    {
        if (expected is null)
            throw new AssertionException("Expected value cannot be null for comparison");

        if (_comparer.Compare(Actual, expected) < 0)
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

        if (_comparer.Compare(Actual, expected) > 0)
            throw new AssertionException(
                $"Expected {Expression ?? "value"} to be at most {ExpectationHelpers.Format(expected)}, but was {ExpectationHelpers.Format(Actual)}");
    }

    /// <summary>
    /// Assert that the actual value is (or is not, if negated) within the specified range (inclusive).
    /// </summary>
    public void toBeInRange(T min, T max)
    {
        if (min is null || max is null)
            throw new AssertionException("Range bounds cannot be null for comparison");

        var inRange = _comparer.Compare(Actual, min) >= 0 && _comparer.Compare(Actual, max) <= 0;

        if (_isNegated)
        {
            if (inRange)
                throw new AssertionException(
                    $"Expected {Expression ?? "value"} to not be in range [{ExpectationHelpers.Format(min)}, {ExpectationHelpers.Format(max)}], but was {ExpectationHelpers.Format(Actual)}");
        }
        else
        {
            if (!inRange)
                throw new AssertionException(
                    $"Expected {Expression ?? "value"} to be in range [{ExpectationHelpers.Format(min)}, {ExpectationHelpers.Format(max)}], but was {ExpectationHelpers.Format(Actual)}");
        }
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
                $"Expected {Expression} to be close to {ExpectationHelpers.Format(expected)} (+/-{ExpectationHelpers.Format(tolerance)}), but was {ExpectationHelpers.Format(Actual)} (diff: {diff.Value})");
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

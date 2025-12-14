namespace DraftSpec;

/// <summary>
/// Expectation wrapper for fluent assertions.
/// </summary>
public class Expectation<T>
{
    private readonly T _actual;
    private readonly string? _expr;

    public Expectation(T actual, string? expr)
    {
        _actual = actual;
        _expr = expr;
    }

    /// <summary>
    /// Assert that the actual value equals the expected value.
    /// </summary>
    public void toBe(T expected)
    {
        if (!Equals(_actual, expected))
            throw new AssertionException(
                $"Expected {_expr} to be {Format(expected)}, but was {Format(_actual)}");
    }

    /// <summary>
    /// Assert that the actual value is null.
    /// </summary>
    public void toBeNull()
    {
        if (_actual is not null)
            throw new AssertionException(
                $"Expected {_expr} to be null, but was {Format(_actual)}");
    }

    /// <summary>
    /// Assert that the actual value is not null.
    /// </summary>
    public void toNotBeNull()
    {
        if (_actual is null)
            throw new AssertionException(
                $"Expected {_expr} to not be null");
    }

    /// <summary>
    /// Assert that the actual value is greater than the expected value.
    /// </summary>
    public void toBeGreaterThan(T expected)
    {
        if (_actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(expected) <= 0)
            throw new AssertionException(
                $"Expected {_expr} to be greater than {Format(expected)}, but was {Format(_actual)}");
    }

    /// <summary>
    /// Assert that the actual value is less than the expected value.
    /// </summary>
    public void toBeLessThan(T expected)
    {
        if (_actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(expected) >= 0)
            throw new AssertionException(
                $"Expected {_expr} to be less than {Format(expected)}, but was {Format(_actual)}");
    }

    /// <summary>
    /// Assert that the actual value is greater than or equal to the expected value.
    /// </summary>
    public void toBeAtLeast(T expected)
    {
        if (_actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(expected) < 0)
            throw new AssertionException(
                $"Expected {_expr} to be at least {Format(expected)}, but was {Format(_actual)}");
    }

    /// <summary>
    /// Assert that the actual value is less than or equal to the expected value.
    /// </summary>
    public void toBeAtMost(T expected)
    {
        if (_actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(expected) > 0)
            throw new AssertionException(
                $"Expected {_expr} to be at most {Format(expected)}, but was {Format(_actual)}");
    }

    /// <summary>
    /// Assert that the actual value is within the specified range (inclusive).
    /// </summary>
    public void toBeInRange(T min, T max)
    {
        if (_actual is not IComparable<T> comparable)
            throw new AssertionException(
                $"Cannot compare {typeof(T).Name} - type does not implement IComparable<{typeof(T).Name}>");

        if (comparable.CompareTo(min) < 0 || comparable.CompareTo(max) > 0)
            throw new AssertionException(
                $"Expected {_expr} to be in range [{Format(min)}, {Format(max)}], but was {Format(_actual)}");
    }

    /// <summary>
    /// Assert that the actual value is close to the expected value within a tolerance.
    /// Works with numeric types (int, double, decimal, etc.).
    /// </summary>
    public void toBeCloseTo(T expected, T tolerance)
    {
        var diff = GetNumericDifference(_actual, expected);
        var tol = ConvertToDouble(tolerance);

        if (diff is null || tol is null)
            throw new AssertionException(
                $"toBeCloseTo requires numeric types, but got {typeof(T).Name}");

        if (diff.Value > tol.Value)
            throw new AssertionException(
                $"Expected {_expr} to be close to {Format(expected)} (±{Format(tolerance)}), but was {Format(_actual)} (diff: {diff.Value})");
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

    private static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => value.ToString() ?? "null"
        };
    }
}

using System.Runtime.CompilerServices;

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

/// <summary>
/// Expectation wrapper for boolean values.
/// </summary>
public class BoolExpectation
{
    private readonly bool _actual;
    private readonly string? _expr;

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
    /// Assert equality (for consistency with Expectation<T>).
    /// </summary>
    public void toBe(bool expected)
    {
        if (_actual != expected)
            throw new AssertionException(
                $"Expected {_expr} to be {expected}, but was {_actual}");
    }
}

/// <summary>
/// Expectation wrapper for string values.
/// </summary>
public class StringExpectation
{
    private readonly string? _actual;
    private readonly string? _expr;

    public StringExpectation(string? actual, string? expr)
    {
        _actual = actual;
        _expr = expr;
    }

    /// <summary>
    /// Assert equality.
    /// </summary>
    public void toBe(string? expected)
    {
        if (_actual != expected)
            throw new AssertionException(
                $"Expected {_expr} to be \"{expected}\", but was \"{_actual}\"");
    }

    /// <summary>
    /// Assert that the string contains a substring.
    /// </summary>
    public void toContain(string substring)
    {
        if (_actual is null || !_actual.Contains(substring))
            throw new AssertionException(
                $"Expected {_expr} to contain \"{substring}\", but was \"{_actual}\"");
    }

    /// <summary>
    /// Assert that the string starts with a prefix.
    /// </summary>
    public void toStartWith(string prefix)
    {
        if (_actual is null || !_actual.StartsWith(prefix))
            throw new AssertionException(
                $"Expected {_expr} to start with \"{prefix}\", but was \"{_actual}\"");
    }

    /// <summary>
    /// Assert that the string ends with a suffix.
    /// </summary>
    public void toEndWith(string suffix)
    {
        if (_actual is null || !_actual.EndsWith(suffix))
            throw new AssertionException(
                $"Expected {_expr} to end with \"{suffix}\", but was \"{_actual}\"");
    }

    /// <summary>
    /// Assert that the string is null or empty.
    /// </summary>
    public void toBeNullOrEmpty()
    {
        if (!string.IsNullOrEmpty(_actual))
            throw new AssertionException(
                $"Expected {_expr} to be null or empty, but was \"{_actual}\"");
    }

    /// <summary>
    /// Assert that the string is null.
    /// </summary>
    public void toBeNull()
    {
        if (_actual is not null)
            throw new AssertionException(
                $"Expected {_expr} to be null, but was \"{_actual}\"");
    }
}

/// <summary>
/// Expectation wrapper for actions (exception testing).
/// </summary>
public class ActionExpectation
{
    private readonly Action _action;
    private readonly string? _expr;

    public ActionExpectation(Action action, string? expr)
    {
        _action = action;
        _expr = expr;
    }

    /// <summary>
    /// Assert that the action throws an exception of the specified type.
    /// </summary>
    public TException toThrow<TException>() where TException : Exception
    {
        try
        {
            _action();
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected {_expr} to throw {typeof(TException).Name}, but threw {ex.GetType().Name}: {ex.Message}");
        }

        throw new AssertionException(
            $"Expected {_expr} to throw {typeof(TException).Name}, but no exception was thrown");
    }

    /// <summary>
    /// Assert that the action throws any exception.
    /// </summary>
    public Exception toThrow()
    {
        try
        {
            _action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new AssertionException(
            $"Expected {_expr} to throw an exception, but no exception was thrown");
    }

    /// <summary>
    /// Assert that the action does not throw any exception.
    /// </summary>
    public void toNotThrow()
    {
        try
        {
            _action();
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected {_expr} to not throw, but threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}

/// <summary>
/// Expectation wrapper for collections.
/// </summary>
public class CollectionExpectation<T>
{
    private readonly IEnumerable<T> _actual;
    private readonly string? _expr;

    public CollectionExpectation(IEnumerable<T> actual, string? expr)
    {
        _actual = actual;
        _expr = expr;
    }

    /// <summary>
    /// Assert that the collection contains the specified item.
    /// </summary>
    public void toContain(T expected)
    {
        if (!_actual.Contains(expected))
            throw new AssertionException(
                $"Expected {_expr} to contain {Format(expected)}, but it did not. Contents: [{FormatCollection()}]");
    }

    /// <summary>
    /// Assert that the collection does not contain the specified item.
    /// </summary>
    public void toNotContain(T expected)
    {
        if (_actual.Contains(expected))
            throw new AssertionException(
                $"Expected {_expr} to not contain {Format(expected)}, but it did");
    }

    /// <summary>
    /// Assert that the collection contains all the specified items.
    /// </summary>
    public void toContainAll(params T[] expected)
    {
        var missing = expected.Where(e => !_actual.Contains(e)).ToList();
        if (missing.Count > 0)
            throw new AssertionException(
                $"Expected {_expr} to contain all of [{string.Join(", ", expected.Select(e => Format(e)))}], but was missing [{string.Join(", ", missing.Select(e => Format(e)))}]");
    }

    /// <summary>
    /// Assert that the collection has the specified count.
    /// </summary>
    public void toHaveCount(int expected)
    {
        var count = _actual.Count();
        if (count != expected)
            throw new AssertionException(
                $"Expected {_expr} to have count {expected}, but was {count}");
    }

    /// <summary>
    /// Assert that the collection is empty.
    /// </summary>
    public void toBeEmpty()
    {
        if (_actual.Any())
            throw new AssertionException(
                $"Expected {_expr} to be empty, but had {_actual.Count()} items: [{FormatCollection()}]");
    }

    /// <summary>
    /// Assert that the collection is not empty.
    /// </summary>
    public void toNotBeEmpty()
    {
        if (!_actual.Any())
            throw new AssertionException(
                $"Expected {_expr} to not be empty");
    }

    /// <summary>
    /// Assert that the collection equals the expected sequence.
    /// </summary>
    public void toBe(IEnumerable<T> expected)
    {
        if (!_actual.SequenceEqual(expected))
            throw new AssertionException(
                $"Expected {_expr} to be [{string.Join(", ", expected.Select(e => Format(e)))}], but was [{FormatCollection()}]");
    }

    /// <summary>
    /// Assert that the collection equals the expected items.
    /// </summary>
    public void toBe(params T[] expected)
    {
        toBe((IEnumerable<T>)expected);
    }

    private string FormatCollection()
    {
        var items = _actual.Take(10).Select(e => Format(e));
        var result = string.Join(", ", items);
        if (_actual.Count() > 10)
            result += ", ...";
        return result;
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

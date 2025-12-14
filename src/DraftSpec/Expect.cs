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

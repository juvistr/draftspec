namespace DraftSpec;

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

namespace DraftSpec;

/// <summary>
/// Expectation wrapper for string values with string-specific assertions.
/// Supports both positive and negated assertions via the <see cref="not"/> property.
/// </summary>
/// <remarks>
/// Created via <c>expect(stringValue)</c>. Provides assertions like <c>toContain()</c>,
/// <c>toStartWith()</c>, <c>toEndWith()</c>, etc.
/// Extension methods can access <see cref="Actual"/> and <see cref="Expression"/>
/// to create custom matchers.
/// </remarks>
public readonly struct StringExpectation
{
    /// <summary>
    /// The actual string value being asserted.
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public string? Actual { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public string? Expression { get; }

    private readonly bool _isNegated;

    /// <summary>
    /// Creates an expectation for the specified string value.
    /// </summary>
    /// <param name="actual">The actual string value to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public StringExpectation(string? actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
        _isNegated = false;
    }

    /// <summary>
    /// Creates an expectation for the specified string value with negation control.
    /// </summary>
    internal StringExpectation(string? actual, string? expr, bool isNegated)
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
    /// expect(str).not.toBe("hello");
    /// expect(str).not.toContain("foo");
    /// </code>
    /// </example>
    public StringExpectation not => new(Actual, Expression, !_isNegated);

    /// <summary>
    /// Assert equality (or inequality, if negated).
    /// </summary>
    public void toBe(string? expected)
    {
        if (_isNegated)
        {
            if (string.Equals(Actual, expected, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to not be \"{expected}\"");
        }
        else
        {
            if (!string.Equals(Actual, expected, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to be \"{expected}\", but was \"{Actual}\"");
        }
    }

    /// <summary>
    /// Assert that the string contains (or does not contain, if negated) a substring.
    /// </summary>
    public void toContain(string substring)
    {
        if (_isNegated)
        {
            if (Actual is not null && Actual.Contains(substring, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to not contain \"{substring}\", but it did");
        }
        else
        {
            if (Actual is null || !Actual.Contains(substring, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to contain \"{substring}\", but was \"{Actual}\"");
        }
    }

    /// <summary>
    /// Assert that the string starts with (or does not start with, if negated) a prefix.
    /// </summary>
    public void toStartWith(string prefix)
    {
        if (_isNegated)
        {
            if (Actual is not null && Actual.StartsWith(prefix, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to not start with \"{prefix}\", but it did");
        }
        else
        {
            if (Actual is null || !Actual.StartsWith(prefix, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to start with \"{prefix}\", but was \"{Actual}\"");
        }
    }

    /// <summary>
    /// Assert that the string ends with (or does not end with, if negated) a suffix.
    /// </summary>
    public void toEndWith(string suffix)
    {
        if (_isNegated)
        {
            if (Actual is not null && Actual.EndsWith(suffix, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to not end with \"{suffix}\", but it did");
        }
        else
        {
            if (Actual is null || !Actual.EndsWith(suffix, StringComparison.Ordinal))
                throw new AssertionException(
                    $"Expected {Expression} to end with \"{suffix}\", but was \"{Actual}\"");
        }
    }

    /// <summary>
    /// Assert that the string is (or is not, if negated) null or empty.
    /// </summary>
    public void toBeNullOrEmpty()
    {
        if (_isNegated)
        {
            if (string.IsNullOrEmpty(Actual))
                throw new AssertionException(
                    $"Expected {Expression} to not be null or empty, but it was");
        }
        else
        {
            if (!string.IsNullOrEmpty(Actual))
                throw new AssertionException(
                    $"Expected {Expression} to be null or empty, but was \"{Actual}\"");
        }
    }

    /// <summary>
    /// Assert that the string is (or is not, if negated) null.
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
                    $"Expected {Expression} to be null, but was \"{Actual}\"");
        }
    }

    /// <summary>
    /// Assert that the string matches (or does not match, if negated) a regular expression pattern.
    /// </summary>
    /// <param name="pattern">The regex pattern to match against.</param>
    public void toMatch(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        // Use 1 second timeout to prevent ReDoS attacks
        var timeout = TimeSpan.FromSeconds(1);

        if (_isNegated)
        {
            if (Actual is not null && System.Text.RegularExpressions.Regex.IsMatch(Actual, pattern, System.Text.RegularExpressions.RegexOptions.None, timeout))
                throw new AssertionException(
                    $"Expected {Expression} to not match pattern \"{pattern}\", but it did");
        }
        else
        {
            if (Actual is null || !System.Text.RegularExpressions.Regex.IsMatch(Actual, pattern, System.Text.RegularExpressions.RegexOptions.None, timeout))
                throw new AssertionException(
                    $"Expected {Expression} to match pattern \"{pattern}\", but was \"{Actual}\"");
        }
    }

    /// <summary>
    /// Assert that the string matches (or does not match, if negated) a regular expression.
    /// </summary>
    /// <param name="regex">The regex to match against.</param>
    public void toMatch(System.Text.RegularExpressions.Regex regex)
    {
        ArgumentNullException.ThrowIfNull(regex);

        if (_isNegated)
        {
            if (Actual is not null && regex.IsMatch(Actual))
                throw new AssertionException(
                    $"Expected {Expression} to not match pattern \"{regex}\", but it did");
        }
        else
        {
            if (Actual is null || !regex.IsMatch(Actual))
                throw new AssertionException(
                    $"Expected {Expression} to match pattern \"{regex}\", but was \"{Actual}\"");
        }
    }

    /// <summary>
    /// Assert that the string has (or does not have, if negated) the expected length.
    /// </summary>
    /// <param name="expectedLength">The expected length.</param>
    public void toHaveLength(int expectedLength)
    {
        if (expectedLength < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedLength), "Length must be non-negative");

        if (_isNegated)
        {
            if (Actual is not null && Actual.Length == expectedLength)
                throw new AssertionException(
                    $"Expected {Expression} to not have length {expectedLength}, but it did");
        }
        else
        {
            if (Actual is null)
                throw new AssertionException(
                    $"Expected {Expression} to have length {expectedLength}, but was null");

            if (Actual.Length != expectedLength)
                throw new AssertionException(
                    $"Expected {Expression} to have length {expectedLength}, but had length {Actual.Length}");
        }
    }
}

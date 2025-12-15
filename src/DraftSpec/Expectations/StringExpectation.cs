namespace DraftSpec;

/// <summary>
/// Expectation wrapper for string values with string-specific assertions.
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

    /// <summary>
    /// Creates an expectation for the specified string value.
    /// </summary>
    /// <param name="actual">The actual string value to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public StringExpectation(string? actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
    }

    /// <summary>
    /// Assert equality.
    /// </summary>
    public void toBe(string? expected)
    {
        if (Actual != expected)
            throw new AssertionException(
                $"Expected {Expression} to be \"{expected}\", but was \"{Actual}\"");
    }

    /// <summary>
    /// Assert that the string contains a substring.
    /// </summary>
    public void toContain(string substring)
    {
        if (Actual is null || !Actual.Contains(substring, StringComparison.Ordinal))
            throw new AssertionException(
                $"Expected {Expression} to contain \"{substring}\", but was \"{Actual}\"");
    }

    /// <summary>
    /// Assert that the string starts with a prefix.
    /// </summary>
    public void toStartWith(string prefix)
    {
        if (Actual is null || !Actual.StartsWith(prefix, StringComparison.Ordinal))
            throw new AssertionException(
                $"Expected {Expression} to start with \"{prefix}\", but was \"{Actual}\"");
    }

    /// <summary>
    /// Assert that the string ends with a suffix.
    /// </summary>
    public void toEndWith(string suffix)
    {
        if (Actual is null || !Actual.EndsWith(suffix, StringComparison.Ordinal))
            throw new AssertionException(
                $"Expected {Expression} to end with \"{suffix}\", but was \"{Actual}\"");
    }

    /// <summary>
    /// Assert that the string is null or empty.
    /// </summary>
    public void toBeNullOrEmpty()
    {
        if (!string.IsNullOrEmpty(Actual))
            throw new AssertionException(
                $"Expected {Expression} to be null or empty, but was \"{Actual}\"");
    }

    /// <summary>
    /// Assert that the string is null.
    /// </summary>
    public void toBeNull()
    {
        if (Actual is not null)
            throw new AssertionException(
                $"Expected {Expression} to be null, but was \"{Actual}\"");
    }
}

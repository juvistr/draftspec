namespace DraftSpec;

/// <summary>
/// Negated expectation wrapper for string assertions.
/// Returned by <c>expect(string).not</c> to enable negative assertions.
/// </summary>
/// <example>
/// <code>
/// expect(str).not.toBe("hello");
/// expect(str).not.toContain("foo");
/// expect(str).not.toBeNullOrEmpty();
/// </code>
/// </example>
public readonly struct NegatedStringExpectation
{
    /// <summary>
    /// The actual string value being asserted.
    /// </summary>
    public string? Actual { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates a negated expectation for the specified string value.
    /// </summary>
    public NegatedStringExpectation(string? actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the string does NOT equal the expected value.
    /// </summary>
    public void toBe(string? expected)
    {
        if (Actual == expected)
            throw new AssertionException(
                $"Expected {Expression} to not be \"{expected}\"");
    }

    /// <summary>
    /// Assert that the string does NOT contain the substring.
    /// </summary>
    public void toContain(string substring)
    {
        if (Actual is not null && Actual.Contains(substring, StringComparison.Ordinal))
            throw new AssertionException(
                $"Expected {Expression} to not contain \"{substring}\", but it did");
    }

    /// <summary>
    /// Assert that the string does NOT start with the prefix.
    /// </summary>
    public void toStartWith(string prefix)
    {
        if (Actual is not null && Actual.StartsWith(prefix, StringComparison.Ordinal))
            throw new AssertionException(
                $"Expected {Expression} to not start with \"{prefix}\", but it did");
    }

    /// <summary>
    /// Assert that the string does NOT end with the suffix.
    /// </summary>
    public void toEndWith(string suffix)
    {
        if (Actual is not null && Actual.EndsWith(suffix, StringComparison.Ordinal))
            throw new AssertionException(
                $"Expected {Expression} to not end with \"{suffix}\", but it did");
    }

    /// <summary>
    /// Assert that the string is NOT null or empty.
    /// </summary>
    public void toBeNullOrEmpty()
    {
        if (string.IsNullOrEmpty(Actual))
            throw new AssertionException(
                $"Expected {Expression} to not be null or empty, but it was");
    }

    /// <summary>
    /// Assert that the string is NOT null.
    /// </summary>
    public void toBeNull()
    {
        if (Actual is null)
            throw new AssertionException(
                $"Expected {Expression} to not be null");
    }

    /// <summary>
    /// Assert that the string does NOT match the regex pattern.
    /// </summary>
    public void toMatch(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (Actual is not null && System.Text.RegularExpressions.Regex.IsMatch(Actual, pattern))
            throw new AssertionException(
                $"Expected {Expression} to not match pattern \"{pattern}\", but it did");
    }

    /// <summary>
    /// Assert that the string does NOT match the regex.
    /// </summary>
    public void toMatch(System.Text.RegularExpressions.Regex regex)
    {
        ArgumentNullException.ThrowIfNull(regex);

        if (Actual is not null && regex.IsMatch(Actual))
            throw new AssertionException(
                $"Expected {Expression} to not match pattern \"{regex}\", but it did");
    }

    /// <summary>
    /// Assert that the string does NOT have the expected length.
    /// </summary>
    public void toHaveLength(int expectedLength)
    {
        if (expectedLength < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedLength), "Length must be non-negative");

        if (Actual is not null && Actual.Length == expectedLength)
            throw new AssertionException(
                $"Expected {Expression} to not have length {expectedLength}, but it did");
    }
}

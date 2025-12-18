namespace DraftSpec;

/// <summary>
/// Negated expectation wrapper for action/exception assertions.
/// Returned by <c>expect(action).not</c> to enable negative assertions.
/// </summary>
/// <example>
/// <code>
/// expect(() => action()).not.toThrow();
/// expect(() => action()).not.toThrow&lt;InvalidOperationException&gt;();
/// </code>
/// </example>
public readonly struct NegatedActionExpectation
{
    /// <summary>
    /// The action being asserted.
    /// </summary>
    public Action Action { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates a negated expectation for the specified action.
    /// </summary>
    public NegatedActionExpectation(Action action, string? expr)
    {
        Action = action;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the action does NOT throw an exception of the specified type.
    /// Passes if no exception is thrown or a different type is thrown.
    /// </summary>
    public void toThrow<TException>() where TException : Exception
    {
        try
        {
            Action();
        }
        catch (TException)
        {
            throw new AssertionException(
                $"Expected {Expression} to not throw {typeof(TException).Name}, but it did");
        }
        catch
        {
            // Different exception type - this is fine
        }
    }

    /// <summary>
    /// Assert that the action does NOT throw any exception.
    /// </summary>
    public void toThrow()
    {
        try
        {
            Action();
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected {Expression} to not throw, but threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}

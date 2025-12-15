namespace DraftSpec;

/// <summary>
/// Expectation wrapper for actions, used for testing exception behavior.
/// </summary>
/// <remarks>
/// Created via <c>expect(() => action())</c>. Provides assertions like <c>toThrow&lt;T&gt;()</c>
/// and <c>toNotThrow()</c>.
/// Extension methods can access <see cref="Action"/> and <see cref="Expression"/>
/// to create custom matchers.
/// </remarks>
public readonly struct ActionExpectation
{
    /// <summary>
    /// The action being asserted.
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public Action Action { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates an expectation for the specified action.
    /// </summary>
    /// <param name="action">The action to execute and assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public ActionExpectation(Action action, string? expr)
    {
        Action = action;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the action throws an exception of the specified type.
    /// </summary>
    public TException toThrow<TException>() where TException : Exception
    {
        try
        {
            Action();
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected {Expression} to throw {typeof(TException).Name}, but threw {ex.GetType().Name}: {ex.Message}");
        }

        throw new AssertionException(
            $"Expected {Expression} to throw {typeof(TException).Name}, but no exception was thrown");
    }

    /// <summary>
    /// Assert that the action throws any exception.
    /// </summary>
    public Exception toThrow()
    {
        try
        {
            Action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new AssertionException(
            $"Expected {Expression} to throw an exception, but no exception was thrown");
    }

    /// <summary>
    /// Assert that the action does not throw any exception.
    /// </summary>
    public void toNotThrow()
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

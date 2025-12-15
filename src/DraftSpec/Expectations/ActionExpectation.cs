namespace DraftSpec;

/// <summary>
/// Expectation wrapper for actions, used for testing exception behavior.
/// </summary>
/// <remarks>
/// Created via <c>expect(() => action())</c>. Provides assertions like <c>toThrow&lt;T&gt;()</c>
/// and <c>toNotThrow()</c>.
/// </remarks>
public class ActionExpectation
{
    private readonly Action _action;
    private readonly string? _expr;

    /// <summary>
    /// Creates an expectation for the specified action.
    /// </summary>
    /// <param name="action">The action to execute and assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
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

namespace DraftSpec;

/// <summary>
/// Expectation wrapper for actions, used for testing exception behavior.
/// Supports both positive and negated assertions via the <see cref="not"/> property.
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

    private readonly bool _isNegated;

    /// <summary>
    /// Creates an expectation for the specified action.
    /// </summary>
    /// <param name="action">The action to execute and assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public ActionExpectation(Action action, string? expr)
    {
        Action = action;
        Expression = expr;
        _isNegated = false;
    }

    /// <summary>
    /// Creates an expectation for the specified action with negation control.
    /// </summary>
    internal ActionExpectation(Action action, string? expr, bool isNegated)
    {
        Action = action;
        Expression = expr;
        _isNegated = isNegated;
    }

    /// <summary>
    /// Returns a negated expectation for chaining negative assertions.
    /// </summary>
    /// <example>
    /// <code>
    /// expect(() => action()).not.toThrow();
    /// expect(() => action()).not.toThrow&lt;InvalidOperationException&gt;();
    /// </code>
    /// </example>
    public ActionExpectation not => new(Action, Expression, !_isNegated);

    /// <summary>
    /// Assert that the action throws (or does not throw, if negated) an exception of the specified type.
    /// </summary>
    /// <returns>The thrown exception (positive only; negated returns default).</returns>
    public TException toThrow<TException>() where TException : Exception
    {
        if (_isNegated)
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
            return default!;
        }
        else
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
    }

    /// <summary>
    /// Assert that the action throws (or does not throw, if negated) any exception.
    /// </summary>
    /// <returns>The thrown exception (positive only; negated returns default).</returns>
    public Exception toThrow()
    {
        if (_isNegated)
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
            return default!;
        }
        else
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

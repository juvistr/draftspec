namespace DraftSpec;

/// <summary>
/// Expectation wrapper for async actions, used for testing async exception behavior.
/// </summary>
/// <remarks>
/// Created via <c>expect(async () => await action())</c>. Provides assertions like
/// <c>toThrowAsync&lt;T&gt;()</c> and <c>toNotThrowAsync()</c>.
/// Extension methods can access <see cref="AsyncAction"/> and <see cref="Expression"/>
/// to create custom matchers.
/// </remarks>
public readonly struct AsyncActionExpectation
{
    /// <summary>
    /// The async action being asserted.
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public Func<Task> AsyncAction { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates an expectation for the specified async action.
    /// </summary>
    /// <param name="asyncAction">The async action to execute and assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public AsyncActionExpectation(Func<Task> asyncAction, string? expr)
    {
        AsyncAction = asyncAction;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the async action throws an exception of the specified type.
    /// </summary>
    /// <returns>The thrown exception for further assertions.</returns>
    public async Task<TException> toThrowAsync<TException>() where TException : Exception
    {
        try
        {
            await AsyncAction();
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
    /// Assert that the async action throws any exception.
    /// </summary>
    /// <returns>The thrown exception for further assertions.</returns>
    public async Task<Exception> toThrowAsync()
    {
        try
        {
            await AsyncAction();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new AssertionException(
            $"Expected {Expression} to throw an exception, but no exception was thrown");
    }

    /// <summary>
    /// Assert that the async action does not throw any exception.
    /// </summary>
    public async Task toNotThrowAsync()
    {
        try
        {
            await AsyncAction();
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected {Expression} to not throw, but threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}

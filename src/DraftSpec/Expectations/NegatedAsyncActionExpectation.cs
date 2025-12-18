namespace DraftSpec;

/// <summary>
/// Negated expectation wrapper for async action/exception assertions.
/// Returned by <c>expect(asyncAction).not</c> to enable negative assertions.
/// </summary>
/// <example>
/// <code>
/// await expect(async () => await action()).not.toThrowAsync();
/// await expect(async () => await action()).not.toThrowAsync&lt;InvalidOperationException&gt;();
/// </code>
/// </example>
public readonly struct NegatedAsyncActionExpectation
{
    /// <summary>
    /// The async action being asserted.
    /// </summary>
    public Func<Task> AsyncAction { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates a negated expectation for the specified async action.
    /// </summary>
    public NegatedAsyncActionExpectation(Func<Task> asyncAction, string? expr)
    {
        AsyncAction = asyncAction;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the async action does NOT throw an exception of the specified type.
    /// Passes if no exception is thrown or a different type is thrown.
    /// </summary>
    public async Task toThrowAsync<TException>() where TException : Exception
    {
        try
        {
            await AsyncAction();
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
    /// Assert that the async action does NOT throw any exception.
    /// </summary>
    public async Task toThrowAsync()
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

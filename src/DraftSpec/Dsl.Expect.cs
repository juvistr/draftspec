using System.Runtime.CompilerServices;

namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Create an expectation for a value.
    /// </summary>
    public static Expectation<T> expect<T>(
        T actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new Expectation<T>(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a boolean value.
    /// </summary>
    public static BoolExpectation expect(
        bool actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new BoolExpectation(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a string value.
    /// </summary>
    public static StringExpectation expect(
        string? actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new StringExpectation(actual, expr);
    }

    /// <summary>
    /// Create an expectation for an action (exception testing).
    /// </summary>
    public static ActionExpectation expect(
        Action action,
        [CallerArgumentExpression("action")] string? expr = null)
    {
        return new ActionExpectation(action, expr);
    }

    /// <summary>
    /// Create an expectation for an async action (async exception testing).
    /// </summary>
    public static AsyncActionExpectation expect(
        Func<Task> asyncAction,
        [CallerArgumentExpression("asyncAction")] string? expr = null)
    {
        return new AsyncActionExpectation(asyncAction, expr);
    }

    /// <summary>
    /// Create an expectation for an array.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        T[] actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new CollectionExpectation<T>(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a list.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        List<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new CollectionExpectation<T>(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a collection.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        IList<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new CollectionExpectation<T>(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a hash set.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        HashSet<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new CollectionExpectation<T>(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a set.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        ISet<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new CollectionExpectation<T>(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a read-only list.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        IReadOnlyList<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new CollectionExpectation<T>(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a read-only collection.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        IReadOnlyCollection<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new CollectionExpectation<T>(actual, expr);
    }

    /// <summary>
    /// Create an expectation for a collection.
    /// </summary>
    public static CollectionExpectation<T> expect<T>(
        ICollection<T> actual,
        [CallerArgumentExpression("actual")] string? expr = null)
    {
        return new CollectionExpectation<T>(actual, expr);
    }
}
using DraftSpec.Expectations;

namespace DraftSpec;

/// <summary>
/// Expectation wrapper for collections.
/// </summary>
public class CollectionExpectation<T>
{
    private readonly IEnumerable<T> _actual;
    private readonly string? _expr;

    public CollectionExpectation(IEnumerable<T> actual, string? expr)
    {
        _actual = actual;
        _expr = expr;
    }

    /// <summary>
    /// Assert that the collection contains the specified item.
    /// </summary>
    public void toContain(T expected)
    {
        if (!_actual.Contains(expected))
            throw new AssertionException(
                $"Expected {_expr} to contain {ExpectationHelpers.Format(expected)}, but it did not. Contents: [{FormatCollection()}]");
    }

    /// <summary>
    /// Assert that the collection does not contain the specified item.
    /// </summary>
    public void toNotContain(T expected)
    {
        if (_actual.Contains(expected))
            throw new AssertionException(
                $"Expected {_expr} to not contain {ExpectationHelpers.Format(expected)}, but it did");
    }

    /// <summary>
    /// Assert that the collection contains all the specified items.
    /// </summary>
    public void toContainAll(params T[] expected)
    {
        var missing = expected.Where(e => !_actual.Contains(e)).ToList();
        if (missing.Count > 0)
            throw new AssertionException(
                $"Expected {_expr} to contain all of [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}], but was missing [{string.Join(", ", missing.Select(e => ExpectationHelpers.Format(e)))}]");
    }

    /// <summary>
    /// Assert that the collection has the specified count.
    /// </summary>
    public void toHaveCount(int expected)
    {
        var count = _actual.Count();
        if (count != expected)
            throw new AssertionException(
                $"Expected {_expr} to have count {expected}, but was {count}");
    }

    /// <summary>
    /// Assert that the collection is empty.
    /// </summary>
    public void toBeEmpty()
    {
        if (_actual.Any())
            throw new AssertionException(
                $"Expected {_expr} to be empty, but had {_actual.Count()} items: [{FormatCollection()}]");
    }

    /// <summary>
    /// Assert that the collection is not empty.
    /// </summary>
    public void toNotBeEmpty()
    {
        if (!_actual.Any())
            throw new AssertionException(
                $"Expected {_expr} to not be empty");
    }

    /// <summary>
    /// Assert that the collection equals the expected sequence.
    /// </summary>
    public void toBe(IEnumerable<T> expected)
    {
        if (!_actual.SequenceEqual(expected))
            throw new AssertionException(
                $"Expected {_expr} to be [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}], but was [{FormatCollection()}]");
    }

    /// <summary>
    /// Assert that the collection equals the expected items.
    /// </summary>
    public void toBe(params T[] expected)
    {
        toBe((IEnumerable<T>)expected);
    }

    private string FormatCollection()
    {
        var items = _actual.Take(10).Select(e => ExpectationHelpers.Format(e));
        var result = string.Join(", ", items);
        if (_actual.Count() > 10)
            result += ", ...";
        return result;
    }
}

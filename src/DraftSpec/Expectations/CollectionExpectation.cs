using DraftSpec.Expectations;

namespace DraftSpec;

/// <summary>
/// Expectation wrapper for collections with collection-specific assertions.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
/// <remarks>
/// Created via <c>expect(collection)</c>. Provides assertions like <c>toContain()</c>,
/// <c>toHaveCount()</c>, <c>toBeEmpty()</c>, etc.
/// Extension methods can access <see cref="Actual"/> and <see cref="Expression"/>
/// to create custom matchers.
/// </remarks>
public readonly struct CollectionExpectation<T>
{
    /// <summary>
    /// The actual collection being asserted.
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public IEnumerable<T> Actual { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// Exposed for extension methods to create custom matchers.
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates an expectation for the specified collection.
    /// </summary>
    /// <param name="actual">The actual collection to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public CollectionExpectation(IEnumerable<T> actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
    }

    /// <summary>
    /// Returns a negated expectation for chaining negative assertions.
    /// </summary>
    /// <example>
    /// <code>
    /// expect(list).not.toContain(item);
    /// expect(list).not.toBeEmpty();
    /// </code>
    /// </example>
    public NegatedCollectionExpectation<T> not => new(Actual, Expression);

    /// <summary>
    /// Assert that the collection contains the specified item.
    /// </summary>
    public void toContain(T expected)
    {
        var materialized = Materialize();
        if (!materialized.Contains(expected))
            throw new AssertionException(
                $"Expected {Expression} to contain {ExpectationHelpers.Format(expected)}, but it did not. Contents: [{FormatCollection(materialized)}]");
    }

    /// <summary>
    /// Assert that the collection does not contain the specified item.
    /// </summary>
    public void toNotContain(T expected)
    {
        if (Actual.Contains(expected))
            throw new AssertionException(
                $"Expected {Expression} to not contain {ExpectationHelpers.Format(expected)}, but it did");
    }

    /// <summary>
    /// Assert that the collection contains all the specified items.
    /// </summary>
    public void toContainAll(params T[] expected)
    {
        var materialized = Materialize();
        var missing = expected.Where(e => !materialized.Contains(e)).ToList();
        if (missing.Count > 0)
            throw new AssertionException(
                $"Expected {Expression} to contain all of [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}], but was missing [{string.Join(", ", missing.Select(e => ExpectationHelpers.Format(e)))}]");
    }

    /// <summary>
    /// Assert that the collection has the specified count.
    /// </summary>
    public void toHaveCount(int expected)
    {
        var materialized = Materialize();
        var count = materialized.Count;
        if (count != expected)
            throw new AssertionException(
                $"Expected {Expression} to have count {expected}, but was {count}");
    }

    /// <summary>
    /// Assert that the collection is empty.
    /// </summary>
    public void toBeEmpty()
    {
        var materialized = Materialize();
        if (materialized.Count > 0)
            throw new AssertionException(
                $"Expected {Expression} to be empty, but had {materialized.Count} items: [{FormatCollection(materialized)}]");
    }

    /// <summary>
    /// Assert that the collection is not empty.
    /// </summary>
    public void toNotBeEmpty()
    {
        var materialized = Materialize();
        if (materialized.Count == 0)
            throw new AssertionException(
                $"Expected {Expression} to not be empty");
    }

    /// <summary>
    /// Assert that the collection equals the expected sequence.
    /// </summary>
    public void toBe(IEnumerable<T> expected)
    {
        var materialized = Materialize();
        if (!materialized.SequenceEqual(expected))
            throw new AssertionException(
                $"Expected {Expression} to be [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}], but was [{FormatCollection(materialized)}]");
    }

    /// <summary>
    /// Assert that the collection equals the expected items.
    /// </summary>
    public void toBe(params T[] expected)
    {
        toBe((IEnumerable<T>)expected);
    }

    /// <summary>
    /// Materializes the collection to avoid multiple enumeration.
    /// Avoids re-materializing if already a collection with O(1) Count.
    /// </summary>
    private IReadOnlyCollection<T> Materialize()
    {
        return Actual as IReadOnlyCollection<T> ?? Actual.ToList();
    }

    private static string FormatCollection(IReadOnlyCollection<T> collection)
    {
        var items = collection.Take(10).Select(e => ExpectationHelpers.Format(e));
        var result = string.Join(", ", items);
        if (collection.Count > 10)
            result += ", ...";
        return result;
    }
}
using DraftSpec.Expectations;

namespace DraftSpec;

/// <summary>
/// Expectation wrapper for collections with collection-specific assertions.
/// Supports both positive and negated assertions via the <see cref="not"/> property.
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

    private readonly bool _isNegated;

    /// <summary>
    /// Creates an expectation for the specified collection.
    /// </summary>
    /// <param name="actual">The actual collection to assert against.</param>
    /// <param name="expr">The expression text (for error messages).</param>
    public CollectionExpectation(IEnumerable<T> actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
        _isNegated = false;
    }

    /// <summary>
    /// Creates an expectation for the specified collection with negation control.
    /// </summary>
    internal CollectionExpectation(IEnumerable<T> actual, string? expr, bool isNegated)
    {
        Actual = actual;
        Expression = expr;
        _isNegated = isNegated;
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
    public CollectionExpectation<T> not => new(Actual, Expression, !_isNegated);

    /// <summary>
    /// Assert that the collection contains (or does not contain, if negated) the specified item.
    /// </summary>
    public void toContain(T expected)
    {
        if (_isNegated)
        {
            if (Actual.Contains(expected))
                throw new AssertionException(
                    $"Expected {Expression} to not contain {ExpectationHelpers.Format(expected)}, but it did");
        }
        else
        {
            var materialized = Materialize();
            if (!materialized.Contains(expected))
                throw new AssertionException(
                    $"Expected {Expression} to contain {ExpectationHelpers.Format(expected)}, but it did not. Contents: [{FormatCollection(materialized)}]");
        }
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
    /// Assert that the collection contains (or does not contain, if negated) all the specified items.
    /// </summary>
    public void toContainAll(params T[] expected)
    {
        var materialized = Materialize();

        if (_isNegated)
        {
            if (expected.All(e => materialized.Contains(e)))
                throw new AssertionException(
                    $"Expected {Expression} to not contain all of [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}], but it did");
        }
        else
        {
            var missing = expected.Where(e => !materialized.Contains(e)).ToList();
            if (missing.Count > 0)
                throw new AssertionException(
                    $"Expected {Expression} to contain all of [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}], but was missing [{string.Join(", ", missing.Select(e => ExpectationHelpers.Format(e)))}]");
        }
    }

    /// <summary>
    /// Assert that the collection has (or does not have, if negated) the specified count.
    /// </summary>
    public void toHaveCount(int expected)
    {
        var materialized = Materialize();
        var count = materialized.Count;

        if (_isNegated)
        {
            if (count == expected)
                throw new AssertionException(
                    $"Expected {Expression} to not have count {expected}, but it did");
        }
        else
        {
            if (count != expected)
                throw new AssertionException(
                    $"Expected {Expression} to have count {expected}, but was {count}");
        }
    }

    /// <summary>
    /// Assert that the collection is (or is not, if negated) empty.
    /// </summary>
    public void toBeEmpty()
    {
        var materialized = Materialize();

        if (_isNegated)
        {
            if (materialized.Count == 0)
                throw new AssertionException(
                    $"Expected {Expression} to not be empty");
        }
        else
        {
            if (materialized.Count > 0)
                throw new AssertionException(
                    $"Expected {Expression} to be empty, but had {materialized.Count} items: [{FormatCollection(materialized)}]");
        }
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
    /// Assert that the collection equals (or does not equal, if negated) the expected sequence.
    /// </summary>
    public void toBe(IEnumerable<T> expected)
    {
        var materialized = Materialize();

        if (_isNegated)
        {
            if (materialized.SequenceEqual(expected))
                throw new AssertionException(
                    $"Expected {Expression} to not be [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}]");
        }
        else
        {
            if (!materialized.SequenceEqual(expected))
                throw new AssertionException(
                    $"Expected {Expression} to be [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}], but was [{FormatCollection(materialized)}]");
        }
    }

    /// <summary>
    /// Assert that the collection equals (or does not equal, if negated) the expected items.
    /// </summary>
    public void toBe(params T[] expected)
    {
        toBe((IEnumerable<T>)expected);
    }

    /// <summary>
    /// Assert that the collection contains (or does not contain, if negated) exactly the specified items (order-independent).
    /// </summary>
    /// <param name="expected">The expected items.</param>
    public void toContainExactly(IEnumerable<T> expected)
    {
        var actualList = Materialize().ToList();
        var expectedList = expected.ToList();

        if (_isNegated)
        {
            if (actualList.Count != expectedList.Count)
                return; // Different count means they don't match exactly - pass

            // Check if they have the same items (accounting for duplicates)
            var actualCopy = new List<T>(actualList);
            foreach (var item in expectedList)
            {
                var index = actualCopy.FindIndex(a => EqualityComparer<T>.Default.Equals(a, item));
                if (index >= 0)
                    actualCopy.RemoveAt(index);
                else
                    return; // Missing item means they don't match exactly - pass
            }

            // If we get here, they match exactly
            throw new AssertionException(
                $"Expected {Expression} to not contain exactly [{string.Join(", ", expectedList.Select(e => ExpectationHelpers.Format(e)))}], but it did");
        }
        else
        {
            if (actualList.Count != expectedList.Count)
            {
                throw new AssertionException(
                    $"Expected {Expression} to contain exactly {expectedList.Count} items, but had {actualList.Count}. " +
                    $"Expected: [{FormatItems(expectedList)}], Actual: [{FormatCollection(actualList)}]");
            }

            // Check that all expected items are present (accounting for duplicates)
            var actualCopy = new List<T>(actualList);
            var missing = new List<T>();

            foreach (var item in expectedList)
            {
                var index = actualCopy.FindIndex(a => EqualityComparer<T>.Default.Equals(a, item));
                if (index >= 0)
                    actualCopy.RemoveAt(index);
                else
                    missing.Add(item);
            }

            if (missing.Count > 0)
            {
                throw new AssertionException(
                    $"Expected {Expression} to contain exactly [{FormatItems(expectedList)}], " +
                    $"but was missing [{FormatItems(missing)}]. " +
                    $"Extra items: [{FormatItems(actualCopy)}]");
            }
        }
    }

    /// <summary>
    /// Assert that the collection contains (or does not contain, if negated) exactly the specified items (order-independent).
    /// </summary>
    /// <param name="expected">The expected items.</param>
    public void toContainExactly(params T[] expected)
    {
        toContainExactly((IEnumerable<T>)expected);
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

    private static string FormatItems(IEnumerable<T> items)
    {
        var itemList = items.Take(10).Select(e => ExpectationHelpers.Format(e)).ToList();
        var result = string.Join(", ", itemList);
        if (items.Skip(10).Any())
            result += ", ...";
        return result;
    }
}

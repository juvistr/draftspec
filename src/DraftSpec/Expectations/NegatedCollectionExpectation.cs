using DraftSpec.Expectations;

namespace DraftSpec;

/// <summary>
/// Negated expectation wrapper for collection assertions.
/// Returned by <c>expect(collection).not</c> to enable negative assertions.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
/// <example>
/// <code>
/// expect(list).not.toContain(item);
/// expect(list).not.toBeEmpty();
/// </code>
/// </example>
public readonly struct NegatedCollectionExpectation<T>
{
    /// <summary>
    /// The actual collection being asserted.
    /// </summary>
    public IEnumerable<T> Actual { get; }

    /// <summary>
    /// The expression text captured from the call site (for error messages).
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// Creates a negated expectation for the specified collection.
    /// </summary>
    public NegatedCollectionExpectation(IEnumerable<T> actual, string? expr)
    {
        Actual = actual;
        Expression = expr;
    }

    /// <summary>
    /// Assert that the collection does NOT contain the specified item.
    /// </summary>
    public void toContain(T expected)
    {
        if (Actual.Contains(expected))
            throw new AssertionException(
                $"Expected {Expression} to not contain {ExpectationHelpers.Format(expected)}, but it did");
    }

    /// <summary>
    /// Assert that the collection does NOT contain all the specified items.
    /// </summary>
    public void toContainAll(params T[] expected)
    {
        var materialized = Materialize();
        if (expected.All(e => materialized.Contains(e)))
            throw new AssertionException(
                $"Expected {Expression} to not contain all of [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}], but it did");
    }

    /// <summary>
    /// Assert that the collection does NOT have the specified count.
    /// </summary>
    public void toHaveCount(int expected)
    {
        var materialized = Materialize();
        var count = materialized.Count;
        if (count == expected)
            throw new AssertionException(
                $"Expected {Expression} to not have count {expected}, but it did");
    }

    /// <summary>
    /// Assert that the collection is NOT empty.
    /// </summary>
    public void toBeEmpty()
    {
        var materialized = Materialize();
        if (materialized.Count == 0)
            throw new AssertionException(
                $"Expected {Expression} to not be empty");
    }

    /// <summary>
    /// Assert that the collection does NOT equal the expected sequence.
    /// </summary>
    public void toBe(IEnumerable<T> expected)
    {
        var materialized = Materialize();
        if (materialized.SequenceEqual(expected))
            throw new AssertionException(
                $"Expected {Expression} to not be [{string.Join(", ", expected.Select(e => ExpectationHelpers.Format(e)))}]");
    }

    /// <summary>
    /// Assert that the collection does NOT equal the expected items.
    /// </summary>
    public void toBe(params T[] expected)
    {
        toBe((IEnumerable<T>)expected);
    }

    /// <summary>
    /// Assert that the collection does NOT contain exactly the specified items (order-independent).
    /// </summary>
    public void toContainExactly(IEnumerable<T> expected)
    {
        var actualList = Materialize().ToList();
        var expectedList = expected.ToList();

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

    /// <summary>
    /// Assert that the collection does NOT contain exactly the specified items (order-independent).
    /// </summary>
    public void toContainExactly(params T[] expected)
    {
        toContainExactly((IEnumerable<T>)expected);
    }

    /// <summary>
    /// Materializes the collection to avoid multiple enumeration.
    /// </summary>
    private IReadOnlyCollection<T> Materialize()
    {
        return Actual as IReadOnlyCollection<T> ?? Actual.ToList();
    }
}

using System.Collections;

namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Tests for CollectionExpectation assertions.
/// </summary>
public class CollectionExpectationTests
{
    #region Single Enumeration Tests

    /// <summary>
    /// Helper that throws on second enumeration to verify single-enumeration behavior.
    /// </summary>
    private class SingleEnumerationSequence<T>(IEnumerable<T> source) : IEnumerable<T>
    {
        private bool _enumerated;

        public IEnumerator<T> GetEnumerator()
        {
            if (_enumerated)
                throw new InvalidOperationException("Sequence was enumerated more than once");
            _enumerated = true;
            return source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Test]
    public async Task toContain_EnumeratesOnlyOnce()
    {
        var sequence = new SingleEnumerationSequence<int>([1, 2, 3]);
        var expectation = new CollectionExpectation<int>(sequence, "seq");

        // Should not throw - only enumerates once
        expectation.toContain(2);
    }

    [Test]
    public async Task toContain_WhenFails_EnumeratesOnlyOnce()
    {
        var sequence = new SingleEnumerationSequence<int>([1, 2, 3]);
        var expectation = new CollectionExpectation<int>(sequence, "seq");

        // Should throw AssertionException, not InvalidOperationException
        var ex = Assert.Throws<AssertionException>(() => expectation.toContain(5));
        await Assert.That(ex.Message).Contains("to contain 5");
    }

    [Test]
    public async Task toContainAll_EnumeratesOnlyOnce()
    {
        var sequence = new SingleEnumerationSequence<int>([1, 2, 3, 4, 5]);
        var expectation = new CollectionExpectation<int>(sequence, "seq");

        expectation.toContainAll(1, 3, 5);
    }

    [Test]
    public async Task toHaveCount_EnumeratesOnlyOnce()
    {
        var sequence = new SingleEnumerationSequence<int>([1, 2, 3]);
        var expectation = new CollectionExpectation<int>(sequence, "seq");

        expectation.toHaveCount(3);
    }

    [Test]
    public async Task toBeEmpty_WhenFails_EnumeratesOnlyOnce()
    {
        var sequence = new SingleEnumerationSequence<int>([1, 2]);
        var expectation = new CollectionExpectation<int>(sequence, "seq");

        // Should throw AssertionException, not InvalidOperationException
        var ex = Assert.Throws<AssertionException>(() => expectation.toBeEmpty());
        await Assert.That(ex.Message).Contains("to be empty");
    }

    [Test]
    public async Task toNotBeEmpty_EnumeratesOnlyOnce()
    {
        var sequence = new SingleEnumerationSequence<int>([1]);
        var expectation = new CollectionExpectation<int>(sequence, "seq");

        expectation.toNotBeEmpty();
    }

    [Test]
    public async Task toBe_EnumeratesOnlyOnce()
    {
        var sequence = new SingleEnumerationSequence<int>([1, 2, 3]);
        var expectation = new CollectionExpectation<int>(sequence, "seq");

        expectation.toBe([1, 2, 3]);
    }

    [Test]
    public async Task toBe_WhenFails_EnumeratesOnlyOnce()
    {
        var sequence = new SingleEnumerationSequence<int>([1, 2, 3]);
        var expectation = new CollectionExpectation<int>(sequence, "seq");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBe([1, 2, 4]));
        await Assert.That(ex.Message).Contains("to be");
    }

    #endregion

    #region toContain

    [Test]
    public async Task toContain_WhenContains_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.toContain(2);
    }

    [Test]
    public async Task toContain_WhenDoesNotContain_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toContain(5));

        await Assert.That(ex.Message).Contains("to contain 5");
        await Assert.That(ex.Message).Contains("Contents:");
    }

    #endregion

    #region toNotContain

    [Test]
    public async Task toNotContain_WhenDoesNotContain_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.toNotContain(5);
    }

    [Test]
    public async Task toNotContain_WhenContains_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toNotContain(2));

        await Assert.That(ex.Message).Contains("to not contain 2");
    }

    #endregion

    #region toContainAll

    [Test]
    public async Task toContainAll_WhenContainsAll_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3, 4, 5], "list");
        expectation.toContainAll(1, 3, 5);
    }

    [Test]
    public async Task toContainAll_WhenMissingSome_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toContainAll(1, 5, 7));

        await Assert.That(ex.Message).Contains("to contain all");
        await Assert.That(ex.Message).Contains("missing");
        await Assert.That(ex.Message).Contains("5");
        await Assert.That(ex.Message).Contains("7");
    }

    #endregion

    #region toHaveCount

    [Test]
    public async Task toHaveCount_WithCorrectCount_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.toHaveCount(3);
    }

    [Test]
    public async Task toHaveCount_WithWrongCount_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toHaveCount(5));

        await Assert.That(ex.Message).Contains("to have count 5");
        await Assert.That(ex.Message).Contains("but was 3");
    }

    [Test]
    public async Task toHaveCount_WithEmptyCollection_PassesForZero()
    {
        var expectation = new CollectionExpectation<int>([], "list");
        expectation.toHaveCount(0);
    }

    #endregion

    #region toBeEmpty / toNotBeEmpty

    [Test]
    public async Task toBeEmpty_WithEmptyCollection_Passes()
    {
        var expectation = new CollectionExpectation<int>([], "list");
        expectation.toBeEmpty();
    }

    [Test]
    public async Task toBeEmpty_WithNonEmptyCollection_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeEmpty());

        await Assert.That(ex.Message).Contains("to be empty");
    }

    [Test]
    public async Task toNotBeEmpty_WithNonEmptyCollection_Passes()
    {
        var expectation = new CollectionExpectation<int>([1], "list");
        expectation.toNotBeEmpty();
    }

    [Test]
    public async Task toNotBeEmpty_WithEmptyCollection_Throws()
    {
        var expectation = new CollectionExpectation<int>([], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toNotBeEmpty());

        await Assert.That(ex.Message).Contains("to not be empty");
    }

    #endregion

    #region toBe (sequence equality)

    [Test]
    public async Task toBe_WithEqualSequence_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.toBe([1, 2, 3]);
    }

    [Test]
    public async Task toBe_WithDifferentSequence_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBe([1, 2, 4]));

        await Assert.That(ex.Message).Contains("to be [1, 2, 4]");
        await Assert.That(ex.Message).Contains("but was [1, 2, 3]");
    }

    [Test]
    public async Task toBe_WithDifferentLength_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2], "list");

        Assert.Throws<AssertionException>(() => expectation.toBe([1, 2, 3]));
    }

    [Test]
    public async Task toBe_WithBothEmpty_Passes()
    {
        var expectation = new CollectionExpectation<int>([], "list");
        expectation.toBe([]);
    }

    [Test]
    public async Task toBe_WithParamsOverload_Passes()
    {
        var expectation = new CollectionExpectation<string>(["a", "b"], "list");
        expectation.toBe("a", "b");
    }

    #endregion

    #region String collections

    [Test]
    public async Task toContain_WithStrings_Passes()
    {
        var expectation = new CollectionExpectation<string>(["apple", "banana", "cherry"], "fruits");
        expectation.toContain("banana");
    }

    [Test]
    public async Task toBe_WithStrings_FormatsCorrectly()
    {
        var expectation = new CollectionExpectation<string>(["a", "b"], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBe(["x", "y"]));

        await Assert.That(ex.Message).Contains("\"x\"");
        await Assert.That(ex.Message).Contains("\"a\"");
    }

    #endregion

    #region expect() overloads for collection types

    [Test]
    public async Task expect_WithHashSet_ReturnsCollectionExpectation()
    {
        var hashSet = new HashSet<int> { 1, 2, 3 };

        DraftSpec.Dsl.expect(hashSet).toHaveCount(3);
        DraftSpec.Dsl.expect(hashSet).toContain(2);
        DraftSpec.Dsl.expect(hashSet).toNotBeEmpty();
    }

    [Test]
    public async Task expect_WithHashSet_toBeEmpty_Works()
    {
        var hashSet = new HashSet<int>();

        DraftSpec.Dsl.expect(hashSet).toBeEmpty();
        DraftSpec.Dsl.expect(hashSet).toHaveCount(0);
    }

    [Test]
    public async Task expect_WithHashSet_toHaveCount_ThrowsOnMismatch()
    {
        var hashSet = new HashSet<int> { 1, 2, 3 };

        var ex = Assert.Throws<AssertionException>(() => DraftSpec.Dsl.expect(hashSet).toHaveCount(5));

        await Assert.That(ex.Message).Contains("to have count 5");
        await Assert.That(ex.Message).Contains("but was 3");
    }

    [Test]
    public async Task expect_WithISet_ReturnsCollectionExpectation()
    {
        ISet<string> set = new HashSet<string> { "a", "b" };

        DraftSpec.Dsl.expect(set).toHaveCount(2);
        DraftSpec.Dsl.expect(set).toContain("a");
    }

    [Test]
    public async Task expect_WithSortedSet_ViaISet_ReturnsCollectionExpectation()
    {
        // SortedSet needs cast to ISet to get collection methods
        // (concrete types match generic overload otherwise)
        ISet<int> sortedSet = new SortedSet<int> { 3, 1, 2 };

        DraftSpec.Dsl.expect(sortedSet).toHaveCount(3);
        DraftSpec.Dsl.expect(sortedSet).toContain(2);
        DraftSpec.Dsl.expect(sortedSet).toBe(1, 2, 3); // SortedSet maintains order
    }

    [Test]
    public async Task expect_WithIReadOnlyList_ReturnsCollectionExpectation()
    {
        IReadOnlyList<int> readOnlyList = new List<int> { 1, 2, 3 }.AsReadOnly();

        DraftSpec.Dsl.expect(readOnlyList).toHaveCount(3);
        DraftSpec.Dsl.expect(readOnlyList).toContain(2);
        DraftSpec.Dsl.expect(readOnlyList).toBe(1, 2, 3);
    }

    [Test]
    public async Task expect_WithIReadOnlyCollection_ReturnsCollectionExpectation()
    {
        IReadOnlyCollection<int> readOnlyCollection = new List<int> { 1, 2, 3 }.AsReadOnly();

        DraftSpec.Dsl.expect(readOnlyCollection).toHaveCount(3);
        DraftSpec.Dsl.expect(readOnlyCollection).toNotBeEmpty();
    }

    [Test]
    public async Task expect_WithICollection_ReturnsCollectionExpectation()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };

        DraftSpec.Dsl.expect(collection).toHaveCount(3);
        DraftSpec.Dsl.expect(collection).toContain(2);
        DraftSpec.Dsl.expect(collection).toNotBeEmpty();
    }

    [Test]
    public async Task expect_WithICollection_toBeEmpty_ThrowsWhenNotEmpty()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };

        var ex = Assert.Throws<AssertionException>(() => DraftSpec.Dsl.expect(collection).toBeEmpty());

        await Assert.That(ex.Message).Contains("to be empty");
    }

    [Test]
    public async Task expect_WithQueue_ViaIReadOnlyCollection_ReturnsCollectionExpectation()
    {
        // Queue<T> implements IReadOnlyCollection<T> but not ICollection<T>
        var queue = new Queue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);

        DraftSpec.Dsl.expect((IReadOnlyCollection<int>)queue).toHaveCount(2);
        DraftSpec.Dsl.expect((IReadOnlyCollection<int>)queue).toContain(1);
    }

    [Test]
    public async Task expect_WithLinkedList_ViaICollection_ReturnsCollectionExpectation()
    {
        // LinkedList<T> implements ICollection<T>
        ICollection<int> linkedList = new LinkedList<int>();
        linkedList.Add(1);
        linkedList.Add(2);
        linkedList.Add(3);

        DraftSpec.Dsl.expect(linkedList).toHaveCount(3);
        DraftSpec.Dsl.expect(linkedList).toNotBeEmpty();
    }

    #endregion
}
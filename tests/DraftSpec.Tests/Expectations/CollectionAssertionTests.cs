namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Tests for additional collection assertions (toContainExactly, toBeInstanceOf, toBeEquivalentTo).
/// </summary>
public class CollectionAssertionTests
{
    #region toContainExactly

    [Test]
    public async Task toContainExactly_WithSameItemsSameOrder_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.toContainExactly(1, 2, 3);
    }

    [Test]
    public async Task toContainExactly_WithSameItemsDifferentOrder_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.toContainExactly(3, 1, 2);
    }

    [Test]
    public async Task toContainExactly_WithDuplicates_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 2, 3], "list");
        expectation.toContainExactly(2, 1, 3, 2);
    }

    [Test]
    public async Task toContainExactly_WithMissingItem_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toContainExactly(1, 2, 4));

        await Assert.That(ex.Message).Contains("missing");
        await Assert.That(ex.Message).Contains("4");
    }

    [Test]
    public async Task toContainExactly_WithExtraItem_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3, 4], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toContainExactly(1, 2, 3));

        await Assert.That(ex.Message).Contains("but had 4");
        await Assert.That(ex.Message).Contains("exactly 3 items");
    }

    [Test]
    public async Task toContainExactly_WithDifferentCount_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.toContainExactly(1, 2, 3));

        await Assert.That(ex.Message).Contains("but had 2");
        await Assert.That(ex.Message).Contains("exactly 3 items");
    }

    [Test]
    public async Task toContainExactly_WithIEnumerable_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        IEnumerable<int> expected = [3, 2, 1];

        expectation.toContainExactly(expected);
    }

    [Test]
    public async Task toContainExactly_WithStrings_Passes()
    {
        var expectation = new CollectionExpectation<string>(["a", "b", "c"], "list");
        expectation.toContainExactly("c", "a", "b");
    }

    [Test]
    public async Task toContainExactly_EmptyCollections_Passes()
    {
        var expectation = new CollectionExpectation<int>([], "list");
        expectation.toContainExactly();
    }

    #endregion

    #region toContainExactly (negated)

    [Test]
    public async Task not_toContainExactly_WithDifferentItems_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.not.toContainExactly(1, 2, 4);
    }

    [Test]
    public async Task not_toContainExactly_WithSameItems_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.not.toContainExactly(3, 1, 2));

        await Assert.That(ex.Message).Contains("to not contain exactly");
    }

    [Test]
    public async Task not_toContainExactly_WithDifferentCount_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.not.toContainExactly(1, 2);
    }

    #endregion

    #region toBeInstanceOf

    [Test]
    public async Task toBeInstanceOf_WithCorrectType_Passes()
    {
        var expectation = new Expectation<object>("hello", "value");

        var result = expectation.toBeInstanceOf<string>();

        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task toBeInstanceOf_WithDerivedType_Passes()
    {
        var ex = new ArgumentNullException("param");
        var expectation = new Expectation<Exception>(ex, "exception");

        var result = expectation.toBeInstanceOf<ArgumentException>();

        await Assert.That(result).IsTypeOf<ArgumentNullException>();
    }

    [Test]
    public async Task toBeInstanceOf_WithWrongType_Throws()
    {
        var expectation = new Expectation<object>(123, "value");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.toBeInstanceOf<string>());

        await Assert.That(ex.Message).Contains("to be instance of String");
        await Assert.That(ex.Message).Contains("was Int32");
    }

    [Test]
    public async Task toBeInstanceOf_WithNull_Throws()
    {
        var expectation = new Expectation<object?>(null, "value");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.toBeInstanceOf<string>());

        await Assert.That(ex.Message).Contains("was null");
    }

    [Test]
    public async Task toBeInstanceOf_ReturnsTypedValue()
    {
        var list = new List<int> { 1, 2, 3 };
        var expectation = new Expectation<object>(list, "collection");

        var result = expectation.toBeInstanceOf<IList<int>>();

        await Assert.That(result.Count).IsEqualTo(3);
    }

    #endregion

    #region toBeInstanceOf (negated)

    [Test]
    public async Task not_toBeInstanceOf_WithWrongType_Passes()
    {
        var expectation = new Expectation<object>(123, "value");
        expectation.not.toBeInstanceOf<string>();
    }

    [Test]
    public async Task not_toBeInstanceOf_WithNull_Passes()
    {
        var expectation = new Expectation<object?>(null, "value");
        expectation.not.toBeInstanceOf<string>();
    }

    [Test]
    public async Task not_toBeInstanceOf_WithCorrectType_Throws()
    {
        var expectation = new Expectation<object>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.not.toBeInstanceOf<string>());

        await Assert.That(ex.Message).Contains("to not be instance of String");
    }

    #endregion

    #region toBeEquivalentTo

    private record Person(string Name, int Age);

    [Test]
    public async Task toBeEquivalentTo_WithEqualObjects_Passes()
    {
        var actual = new Person("Alice", 30);
        var expected = new Person("Alice", 30);
        var expectation = new Expectation<Person>(actual, "person");

        expectation.toBeEquivalentTo(expected);
    }

    [Test]
    public async Task toBeEquivalentTo_WithDifferentInstances_SameValues_Passes()
    {
        var actual = new { Name = "Alice", Age = 30 };
        var expected = new { Name = "Alice", Age = 30 };
        var expectation = new Expectation<object>(actual, "person");

        expectation.toBeEquivalentTo(expected);
    }

    [Test]
    public async Task toBeEquivalentTo_WithDifferentValues_Throws()
    {
        var actual = new Person("Alice", 30);
        var expected = new Person("Bob", 25);
        var expectation = new Expectation<Person>(actual, "person");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.toBeEquivalentTo(expected));

        await Assert.That(ex.Message).Contains("to be equivalent to");
    }

    [Test]
    public async Task toBeEquivalentTo_WithBothNull_Passes()
    {
        var expectation = new Expectation<Person?>(null, "person");
        expectation.toBeEquivalentTo(null);
    }

    [Test]
    public async Task toBeEquivalentTo_WithNullActual_Throws()
    {
        var expectation = new Expectation<Person?>(null, "person");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.toBeEquivalentTo(new Person("Alice", 30)));

        await Assert.That(ex.Message).Contains("was null");
    }

    [Test]
    public async Task toBeEquivalentTo_WithNullExpected_Throws()
    {
        var expectation = new Expectation<Person?>(new Person("Alice", 30), "person");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.toBeEquivalentTo(null));

        await Assert.That(ex.Message).Contains("to be equivalent to null");
    }

    [Test]
    public async Task toBeEquivalentTo_WithNestedObjects_Passes()
    {
        var actual = new { Name = "Alice", Address = new { City = "NYC", Zip = "10001" } };
        var expected = new { Name = "Alice", Address = new { City = "NYC", Zip = "10001" } };
        var expectation = new Expectation<object>(actual, "person");

        expectation.toBeEquivalentTo(expected);
    }

    [Test]
    public async Task toBeEquivalentTo_WithNestedObjects_DifferentValues_Throws()
    {
        var actual = new { Name = "Alice", Address = new { City = "NYC", Zip = "10001" } };
        var expected = new { Name = "Alice", Address = new { City = "LA", Zip = "90001" } };
        var expectation = new Expectation<object>(actual, "person");

        Assert.Throws<AssertionException>(() =>
            expectation.toBeEquivalentTo(expected));
    }

    [Test]
    public async Task toBeEquivalentTo_WithCollections_Passes()
    {
        var actual = new { Items = new[] { 1, 2, 3 } };
        var expected = new { Items = new[] { 1, 2, 3 } };
        var expectation = new Expectation<object>(actual, "obj");

        expectation.toBeEquivalentTo(expected);
    }

    #endregion

    #region toBeEquivalentTo (negated)

    [Test]
    public async Task not_toBeEquivalentTo_WithDifferentValues_Passes()
    {
        var actual = new Person("Alice", 30);
        var expected = new Person("Bob", 25);
        var expectation = new Expectation<Person>(actual, "person");

        expectation.not.toBeEquivalentTo(expected);
    }

    [Test]
    public async Task not_toBeEquivalentTo_WithEqualObjects_Throws()
    {
        var actual = new Person("Alice", 30);
        var expected = new Person("Alice", 30);
        var expectation = new Expectation<Person>(actual, "person");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.not.toBeEquivalentTo(expected));

        await Assert.That(ex.Message).Contains("to not be equivalent to");
    }

    [Test]
    public async Task not_toBeEquivalentTo_WithBothNull_Throws()
    {
        var expectation = new Expectation<Person?>(null, "person");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.not.toBeEquivalentTo(null));

        await Assert.That(ex.Message).Contains("both were null");
    }

    [Test]
    public async Task not_toBeEquivalentTo_WithOneNull_Passes()
    {
        var expectation = new Expectation<Person?>(null, "person");
        expectation.not.toBeEquivalentTo(new Person("Alice", 30));
    }

    #endregion

    #region DSL Integration

    [Test]
    public async Task DSL_expect_collection_toContainExactly()
    {
        var list = new[] { 1, 2, 3 };
        DraftSpec.Dsl.expect(list).toContainExactly(3, 2, 1);
    }

    [Test]
    public async Task DSL_expect_toBeInstanceOf()
    {
        object value = "hello";
        var result = DraftSpec.Dsl.expect(value).toBeInstanceOf<string>();
        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task DSL_expect_toBeEquivalentTo()
    {
        var actual = new { Name = "Test" };
        var expected = new { Name = "Test" };
        DraftSpec.Dsl.expect(actual).toBeEquivalentTo(expected);
    }

    #endregion
}

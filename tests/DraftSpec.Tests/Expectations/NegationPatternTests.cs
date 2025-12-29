namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Tests for the .not negation pattern across all expectation types.
/// </summary>
public class NegationPatternTests
{
    #region Expectation<T>.not

    [Test]
    public async Task GenericExpectation_not_toBe_WhenDifferent_Passes()
    {
        var expectation = new Expectation<int>(5, "value");
        expectation.not.toBe(10);
    }

    [Test]
    public async Task GenericExpectation_not_toBe_WhenEqual_Throws()
    {
        var expectation = new Expectation<int>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBe(5));

        await Assert.That(ex.Message).Contains("to not be 5");
    }

    [Test]
    public async Task GenericExpectation_not_toBeNull_WhenNotNull_Passes()
    {
        var expectation = new Expectation<string>("hello", "value");
        expectation.not.toBeNull();
    }

    [Test]
    public async Task GenericExpectation_not_toBeNull_WhenNull_Throws()
    {
        var expectation = new Expectation<string?>(null, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeNull());

        await Assert.That(ex.Message).Contains("to not be null");
    }

    [Test]
    public async Task GenericExpectation_not_toBeGreaterThan_WhenNotGreater_Passes()
    {
        var expectation = new Expectation<int>(5, "value");
        expectation.not.toBeGreaterThan(10);
    }

    [Test]
    public async Task GenericExpectation_not_toBeGreaterThan_WhenGreater_Throws()
    {
        var expectation = new Expectation<int>(15, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeGreaterThan(10));

        await Assert.That(ex.Message).Contains("to not be greater than");
    }

    [Test]
    public async Task GenericExpectation_not_toBeInRange_WhenOutside_Passes()
    {
        var expectation = new Expectation<int>(20, "value");
        expectation.not.toBeInRange(1, 10);
    }

    [Test]
    public async Task GenericExpectation_not_toBeInRange_WhenInside_Throws()
    {
        var expectation = new Expectation<int>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeInRange(1, 10));

        await Assert.That(ex.Message).Contains("to not be in range");
    }

    #endregion

    #region BoolExpectation.not

    [Test]
    public async Task BoolExpectation_not_toBeTrue_WhenFalse_Passes()
    {
        var expectation = new BoolExpectation(false, "flag");
        expectation.not.toBeTrue();
    }

    [Test]
    public async Task BoolExpectation_not_toBeTrue_WhenTrue_Throws()
    {
        var expectation = new BoolExpectation(true, "flag");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeTrue());

        await Assert.That(ex.Message).Contains("to not be true");
    }

    [Test]
    public async Task BoolExpectation_not_toBeFalse_WhenTrue_Passes()
    {
        var expectation = new BoolExpectation(true, "flag");
        expectation.not.toBeFalse();
    }

    [Test]
    public async Task BoolExpectation_not_toBeFalse_WhenFalse_Throws()
    {
        var expectation = new BoolExpectation(false, "flag");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeFalse());

        await Assert.That(ex.Message).Contains("to not be false");
    }

    [Test]
    public async Task BoolExpectation_not_toBe_WhenDifferent_Passes()
    {
        var expectation = new BoolExpectation(true, "flag");
        expectation.not.toBe(false);
    }

    [Test]
    public async Task BoolExpectation_not_toBe_WhenSame_Throws()
    {
        var expectation = new BoolExpectation(true, "flag");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBe(true));

        await Assert.That(ex.Message).Contains("to not be True");
    }

    #endregion

    #region StringExpectation.not

    [Test]
    public async Task StringExpectation_not_toBe_WhenDifferent_Passes()
    {
        var expectation = new StringExpectation("hello", "str");
        expectation.not.toBe("world");
    }

    [Test]
    public async Task StringExpectation_not_toBe_WhenSame_Throws()
    {
        var expectation = new StringExpectation("hello", "str");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBe("hello"));

        await Assert.That(ex.Message).Contains("to not be");
    }

    [Test]
    public async Task StringExpectation_not_toContain_WhenNotContaining_Passes()
    {
        var expectation = new StringExpectation("hello world", "str");
        expectation.not.toContain("foo");
    }

    [Test]
    public async Task StringExpectation_not_toContain_WhenContaining_Throws()
    {
        var expectation = new StringExpectation("hello world", "str");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toContain("world"));

        await Assert.That(ex.Message).Contains("to not contain");
    }

    [Test]
    public async Task StringExpectation_not_toStartWith_WhenNotStarting_Passes()
    {
        var expectation = new StringExpectation("hello world", "str");
        expectation.not.toStartWith("world");
    }

    [Test]
    public async Task StringExpectation_not_toStartWith_WhenStarting_Throws()
    {
        var expectation = new StringExpectation("hello world", "str");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toStartWith("hello"));

        await Assert.That(ex.Message).Contains("to not start with");
    }

    [Test]
    public async Task StringExpectation_not_toEndWith_WhenNotEnding_Passes()
    {
        var expectation = new StringExpectation("hello world", "str");
        expectation.not.toEndWith("hello");
    }

    [Test]
    public async Task StringExpectation_not_toEndWith_WhenEnding_Throws()
    {
        var expectation = new StringExpectation("hello world", "str");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toEndWith("world"));

        await Assert.That(ex.Message).Contains("to not end with");
    }

    [Test]
    public async Task StringExpectation_not_toBeNullOrEmpty_WhenNotEmpty_Passes()
    {
        var expectation = new StringExpectation("hello", "str");
        expectation.not.toBeNullOrEmpty();
    }

    [Test]
    public async Task StringExpectation_not_toBeNullOrEmpty_WhenEmpty_Throws()
    {
        var expectation = new StringExpectation("", "str");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeNullOrEmpty());

        await Assert.That(ex.Message).Contains("to not be null or empty");
    }

    [Test]
    public async Task StringExpectation_not_toMatch_WhenNotMatching_Passes()
    {
        var expectation = new StringExpectation("hello", "str");
        expectation.not.toMatch(@"\d+");
    }

    [Test]
    public async Task StringExpectation_not_toMatch_WhenMatching_Throws()
    {
        var expectation = new StringExpectation("hello123", "str");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toMatch(@"\d+"));

        await Assert.That(ex.Message).Contains("to not match pattern");
    }

    [Test]
    public async Task StringExpectation_not_toHaveLength_WhenDifferentLength_Passes()
    {
        var expectation = new StringExpectation("hello", "str");
        expectation.not.toHaveLength(10);
    }

    [Test]
    public async Task StringExpectation_not_toHaveLength_WhenSameLength_Throws()
    {
        var expectation = new StringExpectation("hello", "str");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toHaveLength(5));

        await Assert.That(ex.Message).Contains("to not have length 5");
    }

    #endregion

    #region CollectionExpectation<T>.not

    [Test]
    public async Task CollectionExpectation_not_toContain_WhenNotContaining_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.not.toContain(5);
    }

    [Test]
    public async Task CollectionExpectation_not_toContain_WhenContaining_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toContain(2));

        await Assert.That(ex.Message).Contains("to not contain 2");
    }

    [Test]
    public async Task CollectionExpectation_not_toBeEmpty_WhenNotEmpty_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.not.toBeEmpty();
    }

    [Test]
    public async Task CollectionExpectation_not_toBeEmpty_WhenEmpty_Throws()
    {
        var expectation = new CollectionExpectation<int>([], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeEmpty());

        await Assert.That(ex.Message).Contains("to not be empty");
    }

    [Test]
    public async Task CollectionExpectation_not_toHaveCount_WhenDifferentCount_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.not.toHaveCount(5);
    }

    [Test]
    public async Task CollectionExpectation_not_toHaveCount_WhenSameCount_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toHaveCount(3));

        await Assert.That(ex.Message).Contains("to not have count 3");
    }

    [Test]
    public async Task CollectionExpectation_not_toBe_WhenDifferent_Passes()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");
        expectation.not.toBe([1, 2, 4]);
    }

    [Test]
    public async Task CollectionExpectation_not_toBe_WhenSame_Throws()
    {
        var expectation = new CollectionExpectation<int>([1, 2, 3], "list");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBe([1, 2, 3]));

        await Assert.That(ex.Message).Contains("to not be");
    }

    #endregion

    #region ActionExpectation.not

    [Test]
    public async Task ActionExpectation_not_toThrow_WhenNoException_Passes()
    {
        var expectation = new ActionExpectation(() => { }, "action");
        expectation.not.toThrow();
    }

    [Test]
    public async Task ActionExpectation_not_toThrow_WhenThrows_Throws()
    {
        var expectation = new ActionExpectation(() => throw new Exception("oops"), "action");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toThrow());

        await Assert.That(ex.Message).Contains("to not throw");
    }

    [Test]
    public async Task ActionExpectation_not_toThrowGeneric_WhenNoException_Passes()
    {
        var expectation = new ActionExpectation(() => { }, "action");
        expectation.not.toThrow<InvalidOperationException>();
    }

    [Test]
    public async Task ActionExpectation_not_toThrowGeneric_WhenDifferentException_Passes()
    {
        var expectation = new ActionExpectation(() => throw new ArgumentException(), "action");
        expectation.not.toThrow<InvalidOperationException>();
    }

    [Test]
    public async Task ActionExpectation_not_toThrowGeneric_WhenCorrectException_Throws()
    {
        var expectation = new ActionExpectation(
            () => throw new InvalidOperationException("oops"), "action");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.not.toThrow<InvalidOperationException>());

        await Assert.That(ex.Message).Contains("to not throw InvalidOperationException");
    }

    #endregion

    #region AsyncActionExpectation.not

    [Test]
    public async Task AsyncActionExpectation_not_toThrowAsync_WhenNoException_Passes()
    {
        var expectation = new AsyncActionExpectation(async () => await Task.Yield(), "asyncAction");
        await expectation.not.toThrowAsync();
    }

    [Test]
    public async Task AsyncActionExpectation_not_toThrowAsync_WhenThrows_Throws()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                throw new Exception("oops");
            },
            "asyncAction");

        var ex = await Assert.ThrowsAsync<AssertionException>(async () =>
            await expectation.not.toThrowAsync());

        await Assert.That(ex!.Message).Contains("to not throw");
    }

    [Test]
    public async Task AsyncActionExpectation_not_toThrowAsyncGeneric_WhenNoException_Passes()
    {
        var expectation = new AsyncActionExpectation(async () => await Task.Yield(), "asyncAction");
        await expectation.not.toThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task AsyncActionExpectation_not_toThrowAsyncGeneric_WhenDifferentException_Passes()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                throw new ArgumentException();
            },
            "asyncAction");

        await expectation.not.toThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task AsyncActionExpectation_not_toThrowAsyncGeneric_WhenCorrectException_Throws()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("oops");
            },
            "asyncAction");

        var ex = await Assert.ThrowsAsync<AssertionException>(async () =>
            await expectation.not.toThrowAsync<InvalidOperationException>());

        await Assert.That(ex!.Message).Contains("to not throw InvalidOperationException");
    }

    #endregion

    #region DSL Integration

    [Test]
    public async Task DSL_expect_not_toBe_WorksWithChaining()
    {
        // Using the DSL syntax
        int value = 5;
        DraftSpec.Dsl.expect(value).not.toBe(10);
    }

    [Test]
    public async Task DSL_expect_string_not_toContain_WorksWithChaining()
    {
        string str = "hello";
        DraftSpec.Dsl.expect(str).not.toContain("world");
    }

    [Test]
    public async Task DSL_expect_bool_not_toBeTrue_WorksWithChaining()
    {
        bool flag = false;
        DraftSpec.Dsl.expect(flag).not.toBeTrue();
    }

    [Test]
    public async Task DSL_expect_collection_not_toContain_WorksWithChaining()
    {
        var list = new[] { 1, 2, 3 };
        DraftSpec.Dsl.expect(list).not.toContain(5);
    }

    [Test]
    public async Task DSL_expect_action_not_toThrow_WorksWithChaining()
    {
        DraftSpec.Dsl.expect(() => { }).not.toThrow();
    }

    [Test]
    public async Task DSL_expect_asyncAction_not_toThrowAsync_WorksWithChaining()
    {
        await DraftSpec.Dsl.expect(async () => await Task.Yield()).not.toThrowAsync();
    }

    #endregion
}

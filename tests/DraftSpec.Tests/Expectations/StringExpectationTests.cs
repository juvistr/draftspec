namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Tests for StringExpectation assertions.
/// </summary>
public class StringExpectationTests
{
    #region toBe

    [Test]
    public async Task toBe_WithEqualStrings_Passes()
    {
        var expectation = new StringExpectation("hello", "value");
        expectation.toBe("hello");
    }

    [Test]
    public async Task toBe_WithDifferentStrings_Throws()
    {
        var expectation = new StringExpectation("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBe("world"));

        await Assert.That(ex.Message).Contains("to be \"world\"");
        await Assert.That(ex.Message).Contains("but was \"hello\"");
    }

    [Test]
    public async Task toBe_WithBothNull_Passes()
    {
        var expectation = new StringExpectation(null, "value");
        expectation.toBe(null);
    }

    [Test]
    public async Task toBe_WithNullActualNonNullExpected_Throws()
    {
        var expectation = new StringExpectation(null, "value");

        Assert.Throws<AssertionException>(() => expectation.toBe("hello"));
    }

    #endregion

    #region toContain

    [Test]
    public async Task toContain_WhenContainsSubstring_Passes()
    {
        var expectation = new StringExpectation("hello world", "value");
        expectation.toContain("world");
    }

    [Test]
    public async Task toContain_WhenDoesNotContain_Throws()
    {
        var expectation = new StringExpectation("hello world", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toContain("foo"));

        await Assert.That(ex.Message).Contains("to contain \"foo\"");
    }

    [Test]
    public async Task toContain_WithNullActual_Throws()
    {
        var expectation = new StringExpectation(null, "value");

        Assert.Throws<AssertionException>(() => expectation.toContain("foo"));
    }

    #endregion

    #region toStartWith

    [Test]
    public async Task toStartWith_WhenStartsWith_Passes()
    {
        var expectation = new StringExpectation("hello world", "value");
        expectation.toStartWith("hello");
    }

    [Test]
    public async Task toStartWith_WhenDoesNotStartWith_Throws()
    {
        var expectation = new StringExpectation("hello world", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toStartWith("world"));

        await Assert.That(ex.Message).Contains("to start with \"world\"");
    }

    [Test]
    public async Task toStartWith_WithNullActual_Throws()
    {
        var expectation = new StringExpectation(null, "value");

        Assert.Throws<AssertionException>(() => expectation.toStartWith("hello"));
    }

    #endregion

    #region toEndWith

    [Test]
    public async Task toEndWith_WhenEndsWith_Passes()
    {
        var expectation = new StringExpectation("hello world", "value");
        expectation.toEndWith("world");
    }

    [Test]
    public async Task toEndWith_WhenDoesNotEndWith_Throws()
    {
        var expectation = new StringExpectation("hello world", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toEndWith("hello"));

        await Assert.That(ex.Message).Contains("to end with \"hello\"");
    }

    [Test]
    public async Task toEndWith_WithNullActual_Throws()
    {
        var expectation = new StringExpectation(null, "value");

        Assert.Throws<AssertionException>(() => expectation.toEndWith("world"));
    }

    #endregion

    #region toBeNullOrEmpty

    [Test]
    public async Task toBeNullOrEmpty_WithNull_Passes()
    {
        var expectation = new StringExpectation(null, "value");
        expectation.toBeNullOrEmpty();
    }

    [Test]
    public async Task toBeNullOrEmpty_WithEmptyString_Passes()
    {
        var expectation = new StringExpectation("", "value");
        expectation.toBeNullOrEmpty();
    }

    [Test]
    public async Task toBeNullOrEmpty_WithNonEmptyString_Throws()
    {
        var expectation = new StringExpectation("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeNullOrEmpty());

        await Assert.That(ex.Message).Contains("to be null or empty");
    }

    #endregion

    #region toBeNull

    [Test]
    public async Task toBeNull_WithNull_Passes()
    {
        var expectation = new StringExpectation(null, "value");
        expectation.toBeNull();
    }

    [Test]
    public async Task toBeNull_WithNonNull_Throws()
    {
        var expectation = new StringExpectation("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeNull());

        await Assert.That(ex.Message).Contains("to be null");
    }

    [Test]
    public async Task toBeNull_WithEmptyString_Throws()
    {
        var expectation = new StringExpectation("", "value");

        Assert.Throws<AssertionException>(() => expectation.toBeNull());
    }

    #endregion
}
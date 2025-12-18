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

    #region toMatch (string pattern)

    [Test]
    public async Task toMatch_WhenPatternMatches_Passes()
    {
        var expectation = new StringExpectation("hello123world", "value");
        expectation.toMatch(@"\d+");
    }

    [Test]
    public async Task toMatch_WhenPatternDoesNotMatch_Throws()
    {
        var expectation = new StringExpectation("hello world", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toMatch(@"\d+"));

        await Assert.That(ex.Message).Contains("to match pattern");
    }

    [Test]
    public async Task toMatch_WithNullActual_Throws()
    {
        var expectation = new StringExpectation(null, "value");

        Assert.Throws<AssertionException>(() => expectation.toMatch(@"\d+"));
    }

    [Test]
    public async Task toMatch_WithNullPattern_ThrowsArgumentNull()
    {
        var expectation = new StringExpectation("hello", "value");

        Assert.Throws<ArgumentNullException>(() => expectation.toMatch((string)null!));
    }

    [Test]
    public async Task toMatch_WithComplexPattern_Passes()
    {
        var expectation = new StringExpectation("user@example.com", "email");
        expectation.toMatch(@"^[\w.+-]+@[\w-]+\.[\w.-]+$");
    }

    [Test]
    public async Task toMatch_WithAnchoredPattern_WorksCorrectly()
    {
        var expectation = new StringExpectation("hello", "value");

        // Should match when pattern is not anchored
        expectation.toMatch("ell");

        // Should fail when anchored pattern doesn't match
        Assert.Throws<AssertionException>(() => expectation.toMatch("^ell$"));
    }

    #endregion

    #region toMatch (Regex)

    [Test]
    public async Task toMatch_WithRegex_WhenMatches_Passes()
    {
        var expectation = new StringExpectation("HELLO123", "value");
        var regex = new System.Text.RegularExpressions.Regex(@"\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        expectation.toMatch(regex);
    }

    [Test]
    public async Task toMatch_WithRegex_WhenDoesNotMatch_Throws()
    {
        var expectation = new StringExpectation("hello", "value");
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");

        var ex = Assert.Throws<AssertionException>(() => expectation.toMatch(regex));

        await Assert.That(ex.Message).Contains("to match pattern");
    }

    [Test]
    public async Task toMatch_WithRegex_NullActual_Throws()
    {
        var expectation = new StringExpectation(null, "value");
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");

        Assert.Throws<AssertionException>(() => expectation.toMatch(regex));
    }

    [Test]
    public async Task toMatch_WithNullRegex_ThrowsArgumentNull()
    {
        var expectation = new StringExpectation("hello", "value");

        Assert.Throws<ArgumentNullException>(() => expectation.toMatch((System.Text.RegularExpressions.Regex)null!));
    }

    #endregion

    #region toHaveLength

    [Test]
    public async Task toHaveLength_WhenLengthMatches_Passes()
    {
        var expectation = new StringExpectation("hello", "value");
        expectation.toHaveLength(5);
    }

    [Test]
    public async Task toHaveLength_WhenLengthDoesNotMatch_Throws()
    {
        var expectation = new StringExpectation("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toHaveLength(3));

        await Assert.That(ex.Message).Contains("to have length 3");
        await Assert.That(ex.Message).Contains("had length 5");
    }

    [Test]
    public async Task toHaveLength_WithNullActual_Throws()
    {
        var expectation = new StringExpectation(null, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toHaveLength(5));

        await Assert.That(ex.Message).Contains("was null");
    }

    [Test]
    public async Task toHaveLength_WithEmptyString_PassesForZero()
    {
        var expectation = new StringExpectation("", "value");
        expectation.toHaveLength(0);
    }

    [Test]
    public async Task toHaveLength_WithNegativeLength_ThrowsArgumentOutOfRange()
    {
        var expectation = new StringExpectation("hello", "value");

        Assert.Throws<ArgumentOutOfRangeException>(() => expectation.toHaveLength(-1));
    }

    [Test]
    public async Task toHaveLength_WithUnicodeString_CountsCharacters()
    {
        // "Hello 🌍" has 8 characters (including the emoji which is 1 character in .NET)
        var expectation = new StringExpectation("Hello 🌍", "value");
        expectation.toHaveLength(8);
    }

    #endregion
}
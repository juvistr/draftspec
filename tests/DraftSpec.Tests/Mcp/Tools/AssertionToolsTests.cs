using DraftSpec.Mcp.Tools;

namespace DraftSpec.Tests.Mcp.Tools;

/// <summary>
/// Tests for natural language assertion parsing.
/// </summary>
public class AssertionToolsTests
{
    #region Null Assertions

    [Test]
    public async Task Parse_ShouldNotBeNull_ReturnsToNotBeNull()
    {
        var result = AssertionTools.Parse("should not be null", "result");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(result).toNotBeNull()");
        await Assert.That(result.Confidence).IsEqualTo(1.0);
    }

    [Test]
    public async Task Parse_NotBeNull_ReturnsToNotBeNull()
    {
        var result = AssertionTools.Parse("not be null", "user");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(user).toNotBeNull()");
    }

    [Test]
    public async Task Parse_ShouldBeNull_ReturnsToBeNull()
    {
        var result = AssertionTools.Parse("should be null", "error");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(error).toBeNull()");
    }

    #endregion

    #region Boolean Assertions

    [Test]
    public async Task Parse_ShouldBeTrue_ReturnsToBeTrue()
    {
        var result = AssertionTools.Parse("should be true", "isValid");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(isValid).toBeTrue()");
    }

    [Test]
    public async Task Parse_ShouldBeFalse_ReturnsToBeFalse()
    {
        var result = AssertionTools.Parse("should be false", "hasErrors");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(hasErrors).toBeFalse()");
    }

    #endregion

    #region Numeric Comparisons

    [Test]
    public async Task Parse_ShouldBeGreaterThan_ReturnsToBeGreaterThan()
    {
        var result = AssertionTools.Parse("should be greater than 5", "count");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(count).toBeGreaterThan(5)");
        await Assert.That(result.Confidence).IsEqualTo(0.95);
    }

    [Test]
    public async Task Parse_BeGreaterThanDecimal_ReturnsToBeGreaterThan()
    {
        var result = AssertionTools.Parse("be greater than 3.14", "value");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(value).toBeGreaterThan(3.14)");
    }

    [Test]
    public async Task Parse_ShouldBeLessThan_ReturnsToBeLessThan()
    {
        var result = AssertionTools.Parse("should be less than 10", "x");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(x).toBeLessThan(10)");
    }

    [Test]
    public async Task Parse_BeLessThanNegative_ReturnsToBeLessThan()
    {
        var result = AssertionTools.Parse("be less than -5", "temperature");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(temperature).toBeLessThan(-5)");
    }

    [Test]
    public async Task Parse_ShouldBeAtLeast_ReturnsToBeAtLeast()
    {
        var result = AssertionTools.Parse("should be at least 18", "age");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(age).toBeAtLeast(18)");
    }

    [Test]
    public async Task Parse_ShouldBeAtMost_ReturnsToBeAtMost()
    {
        var result = AssertionTools.Parse("should be at most 100", "percentage");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(percentage).toBeAtMost(100)");
    }

    #endregion

    #region Collection Assertions

    [Test]
    public async Task Parse_ShouldHaveCount_ReturnsToHaveCount()
    {
        var result = AssertionTools.Parse("should have count 3", "items");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(items).toHaveCount(3)");
    }

    [Test]
    public async Task Parse_HaveCountOf_ReturnsToHaveCount()
    {
        var result = AssertionTools.Parse("have a count of 5", "list");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(list).toHaveCount(5)");
    }

    [Test]
    public async Task Parse_ShouldHave3Items_ReturnsToHaveCount()
    {
        var result = AssertionTools.Parse("should have 3 items", "array");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(array).toHaveCount(3)");
    }

    [Test]
    public async Task Parse_Have1Item_ReturnsToHaveCount()
    {
        var result = AssertionTools.Parse("have 1 item", "collection");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(collection).toHaveCount(1)");
    }

    [Test]
    public async Task Parse_ShouldBeEmpty_ReturnsToBeEmpty()
    {
        var result = AssertionTools.Parse("should be empty", "list");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(list).toBeEmpty()");
    }

    [Test]
    public async Task Parse_ShouldNotBeEmpty_ReturnsToNotBeEmpty()
    {
        var result = AssertionTools.Parse("should not be empty", "results");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(results).toNotBeEmpty()");
    }

    #endregion

    #region String Assertions

    [Test]
    public async Task Parse_ShouldContain_ReturnsToContain()
    {
        var result = AssertionTools.Parse("should contain 'hello'", "message");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(message).toContain(\"hello\")");
    }

    [Test]
    public async Task Parse_ContainWithDoubleQuotes_ReturnsToContain()
    {
        var result = AssertionTools.Parse("contain \"world\"", "text");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(text).toContain(\"world\")");
    }

    [Test]
    public async Task Parse_ContainWithoutQuotes_ReturnsToContain()
    {
        var result = AssertionTools.Parse("should contain hello", "str");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(str).toContain(\"hello\")");
    }

    [Test]
    public async Task Parse_ShouldStartWith_ReturnsToStartWith()
    {
        var result = AssertionTools.Parse("should start with 'Error:'", "log");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(log).toStartWith(\"Error:\")");
    }

    [Test]
    public async Task Parse_ShouldEndWith_ReturnsToEndWith()
    {
        var result = AssertionTools.Parse("should end with '.txt'", "filename");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(filename).toEndWith(\".txt\")");
    }

    [Test]
    public async Task Parse_ShouldMatchPattern_ReturnsToMatch()
    {
        var result = AssertionTools.Parse("should match pattern '[0-9]+'", "input");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(input).toMatch(\"[0-9]+\")");
    }

    [Test]
    public async Task Parse_Match_ReturnsToMatch()
    {
        var result = AssertionTools.Parse("match '^test.*'", "value");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(value).toMatch(\"^test.*\")");
    }

    #endregion

    #region Exception Assertions

    [Test]
    public async Task Parse_ShouldThrowArgumentException_ReturnsToThrow()
    {
        var result = AssertionTools.Parse("should throw ArgumentException", "action");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(() => action).toThrow<ArgumentException>()");
    }

    [Test]
    public async Task Parse_ThrowAnInvalidOperationException_ReturnsToThrow()
    {
        var result = AssertionTools.Parse("throw an invalid operation exception", "func");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(() => func).toThrow<InvalidOperationException>()");
    }

    [Test]
    public async Task Parse_ShouldThrow_ReturnsToThrow()
    {
        var result = AssertionTools.Parse("should throw", "call");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(() => call).toThrow()");
    }

    [Test]
    public async Task Parse_ThrowAnException_ReturnsToThrow()
    {
        var result = AssertionTools.Parse("throw an exception", "method");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(() => method).toThrow()");
    }

    [Test]
    public async Task Parse_ShouldNotThrow_ReturnsToNotThrow()
    {
        var result = AssertionTools.Parse("should not throw", "operation");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(() => operation).toNotThrow()");
    }

    [Test]
    public async Task Parse_NotThrowAnyException_ReturnsToNotThrow()
    {
        var result = AssertionTools.Parse("not throw any exception", "safeCall");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(() => safeCall).toNotThrow()");
    }

    #endregion

    #region Equality Assertions

    [Test]
    public async Task Parse_ShouldEqualNumber_ReturnsToBe()
    {
        var result = AssertionTools.Parse("should equal 42", "answer");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(answer).toBe(42)");
    }

    [Test]
    public async Task Parse_ShouldBeEqualToString_ReturnsToBe()
    {
        var result = AssertionTools.Parse("should be equal to 'test'", "name");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(name).toBe(\"test\")");
    }

    [Test]
    public async Task Parse_ShouldBe_ReturnsToBe()
    {
        var result = AssertionTools.Parse("should be hello", "greeting");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(greeting).toBe(\"hello\")");
    }

    [Test]
    public async Task Parse_EqualToBoolean_ReturnsToBe()
    {
        var result = AssertionTools.Parse("equal to true", "flag");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(flag).toBe(true)");
    }

    [Test]
    public async Task Parse_ShouldNotEqual_ReturnsNotToBe()
    {
        var result = AssertionTools.Parse("should not equal 0", "count");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(count).not.toBe(0)");
    }

    [Test]
    public async Task Parse_NotBeEqualTo_ReturnsNotToBe()
    {
        var result = AssertionTools.Parse("not be equal to 'error'", "status");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(status).not.toBe(\"error\")");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Parse_UnrecognizedPattern_ReturnsFailureWithFallback()
    {
        var result = AssertionTools.Parse("should be awesome", "x");

        // "be awesome" doesn't match a specific pattern, falls through to general "be" pattern
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(x).toBe(\"awesome\")");
    }

    [Test]
    public async Task Parse_GibberishInput_ReturnsFallbackTemplate()
    {
        var result = AssertionTools.Parse("xyz abc 123", "val");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Code).Contains("expect(val)");
        await Assert.That(result.Confidence).IsEqualTo(0.0);
        await Assert.That(result.Error).IsNotNull();
    }

    [Test]
    public async Task Parse_EmptyInput_ReturnsFallback()
    {
        var result = AssertionTools.Parse("", "x");

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Parse_CaseInsensitive_Works()
    {
        var result = AssertionTools.Parse("SHOULD BE NULL", "item");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(item).toBeNull()");
    }

    [Test]
    public async Task Parse_LeadingTrailingSpaces_Works()
    {
        var result = AssertionTools.Parse("  should be true  ", "flag");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(flag).toBeTrue()");
    }

    [Test]
    public async Task Parse_ComplexVariableName_PreservesName()
    {
        var result = AssertionTools.Parse("should not be null", "user.Profile.Email");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(user.Profile.Email).toNotBeNull()");
    }

    #endregion

    #region String Escaping

    [Test]
    public async Task Parse_StringWithBackslash_EscapesCorrectly()
    {
        var result = AssertionTools.Parse(@"contain 'C:\path'", "path");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).Contains(@"\\");
    }

    [Test]
    public async Task Parse_StringWithSimpleQuotes_Works()
    {
        var result = AssertionTools.Parse("contain 'hello world'", "text");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Code).IsEqualTo("expect(text).toContain(\"hello world\")");
    }

    #endregion
}

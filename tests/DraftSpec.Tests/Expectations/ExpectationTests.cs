namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Tests for Expectation&lt;T&gt; generic assertions.
/// </summary>
public class ExpectationTests
{
    #region toBe

    [Test]
    public async Task toBe_WithEqualInts_Passes()
    {
        var expectation = new Expectation<int>(42, "value");
        expectation.toBe(42); // Should not throw
    }

    [Test]
    public async Task toBe_WithDifferentInts_ThrowsWithMessage()
    {
        var expectation = new Expectation<int>(42, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBe(99));

        await Assert.That(ex.Message).Contains("to be 99");
        await Assert.That(ex.Message).Contains("but was 42");
    }

    [Test]
    public async Task toBe_WithEqualStrings_Passes()
    {
        var expectation = new Expectation<string>("hello", "value");
        expectation.toBe("hello");
    }

    [Test]
    public async Task toBe_WithNullValues_Passes()
    {
        var expectation = new Expectation<string?>(null, "value");
        expectation.toBe(null);
    }

    #endregion

    #region toBeNull / toNotBeNull

    [Test]
    public async Task toBeNull_WithNullValue_Passes()
    {
        var expectation = new Expectation<string?>(null, "value");
        expectation.toBeNull();
    }

    [Test]
    public async Task toBeNull_WithNonNullValue_Throws()
    {
        var expectation = new Expectation<string?>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeNull());

        await Assert.That(ex.Message).Contains("to be null");
    }

    [Test]
    public async Task toNotBeNull_WithNonNullValue_Passes()
    {
        var expectation = new Expectation<string?>("hello", "value");
        expectation.toNotBeNull();
    }

    [Test]
    public async Task toNotBeNull_WithNullValue_Throws()
    {
        var expectation = new Expectation<string?>(null, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toNotBeNull());

        await Assert.That(ex.Message).Contains("to not be null");
    }

    #endregion

    #region toBeGreaterThan / toBeLessThan

    [Test]
    public async Task toBeGreaterThan_WhenGreater_Passes()
    {
        var expectation = new Expectation<int>(10, "value");
        expectation.toBeGreaterThan(5);
    }

    [Test]
    public async Task toBeGreaterThan_WhenEqual_Throws()
    {
        var expectation = new Expectation<int>(10, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeGreaterThan(10));

        await Assert.That(ex.Message).Contains("to be greater than 10");
    }

    [Test]
    public async Task toBeGreaterThan_WhenLess_Throws()
    {
        var expectation = new Expectation<int>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeGreaterThan(10));

        await Assert.That(ex.Message).Contains("to be greater than 10");
        await Assert.That(ex.Message).Contains("but was 5");
    }

    [Test]
    public async Task toBeLessThan_WhenLess_Passes()
    {
        var expectation = new Expectation<int>(5, "value");
        expectation.toBeLessThan(10);
    }

    [Test]
    public async Task toBeLessThan_WhenEqual_Throws()
    {
        var expectation = new Expectation<int>(10, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeLessThan(10));

        await Assert.That(ex.Message).Contains("to be less than 10");
    }

    [Test]
    public async Task toBeLessThan_WhenGreater_Throws()
    {
        var expectation = new Expectation<int>(15, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeLessThan(10));

        await Assert.That(ex.Message).Contains("to be less than 10");
    }

    #endregion

    #region toBeAtLeast / toBeAtMost

    [Test]
    public async Task toBeAtLeast_WhenGreater_Passes()
    {
        var expectation = new Expectation<int>(10, "value");
        expectation.toBeAtLeast(5);
    }

    [Test]
    public async Task toBeAtLeast_WhenEqual_Passes()
    {
        var expectation = new Expectation<int>(10, "value");
        expectation.toBeAtLeast(10);
    }

    [Test]
    public async Task toBeAtLeast_WhenLess_Throws()
    {
        var expectation = new Expectation<int>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeAtLeast(10));

        await Assert.That(ex.Message).Contains("to be at least 10");
    }

    [Test]
    public async Task toBeAtMost_WhenLess_Passes()
    {
        var expectation = new Expectation<int>(5, "value");
        expectation.toBeAtMost(10);
    }

    [Test]
    public async Task toBeAtMost_WhenEqual_Passes()
    {
        var expectation = new Expectation<int>(10, "value");
        expectation.toBeAtMost(10);
    }

    [Test]
    public async Task toBeAtMost_WhenGreater_Throws()
    {
        var expectation = new Expectation<int>(15, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeAtMost(10));

        await Assert.That(ex.Message).Contains("to be at most 10");
    }

    #endregion

    #region toBeInRange

    [Test]
    public async Task toBeInRange_WhenInRange_Passes()
    {
        var expectation = new Expectation<int>(5, "value");
        expectation.toBeInRange(1, 10);
    }

    [Test]
    public async Task toBeInRange_WhenAtMin_Passes()
    {
        var expectation = new Expectation<int>(1, "value");
        expectation.toBeInRange(1, 10);
    }

    [Test]
    public async Task toBeInRange_WhenAtMax_Passes()
    {
        var expectation = new Expectation<int>(10, "value");
        expectation.toBeInRange(1, 10);
    }

    [Test]
    public async Task toBeInRange_WhenBelowMin_Throws()
    {
        var expectation = new Expectation<int>(0, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeInRange(1, 10));

        await Assert.That(ex.Message).Contains("to be in range [1, 10]");
    }

    [Test]
    public async Task toBeInRange_WhenAboveMax_Throws()
    {
        var expectation = new Expectation<int>(15, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeInRange(1, 10));

        await Assert.That(ex.Message).Contains("to be in range [1, 10]");
    }

    #endregion

    #region toBeCloseTo

    [Test]
    public async Task toBeCloseTo_WhenWithinTolerance_Passes()
    {
        var expectation = new Expectation<double>(10.05, "value");
        expectation.toBeCloseTo(10.0, 0.1);
    }

    [Test]
    public async Task toBeCloseTo_WhenExactlyEqual_Passes()
    {
        var expectation = new Expectation<double>(10.0, "value");
        expectation.toBeCloseTo(10.0, 0.01);
    }

    [Test]
    public async Task toBeCloseTo_WhenOutsideTolerance_Throws()
    {
        var expectation = new Expectation<double>(10.5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeCloseTo(10.0, 0.1));

        await Assert.That(ex.Message).Contains("to be close to");
    }

    [Test]
    public async Task toBeCloseTo_WithInts_Passes()
    {
        var expectation = new Expectation<int>(10, "value");
        expectation.toBeCloseTo(11, 2);
    }

    [Test]
    public async Task toBeCloseTo_WithNonNumericType_Throws()
    {
        var expectation = new Expectation<string>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeCloseTo("world", "x"));

        await Assert.That(ex.Message).Contains("toBeCloseTo requires numeric types");
    }

    [Test]
    public async Task toBeCloseTo_WithPositiveInfinity_PassesWhenBothInfinity()
    {
        var expectation = new Expectation<double>(double.PositiveInfinity, "value");

        // Infinity - Infinity = NaN, but NaN > tolerance returns false,
        // so the assertion passes (this is the actual behavior)
        expectation.toBeCloseTo(double.PositiveInfinity, 0.1);
    }

    [Test]
    public async Task toBeCloseTo_WithNaN_PassesDueToNaNComparison()
    {
        var expectation = new Expectation<double>(double.NaN, "value");

        // NaN - any number = NaN, and NaN > tolerance returns false,
        // so the assertion passes (this is edge case behavior)
        expectation.toBeCloseTo(5.0, 0.1);
    }

    [Test]
    public async Task toBeCloseTo_WithInfinityAndFinite_Throws()
    {
        var expectation = new Expectation<double>(double.PositiveInfinity, "value");

        // Infinity - finite = Infinity, which is > any tolerance
        var ex = Assert.Throws<AssertionException>(() => expectation.toBeCloseTo(5.0, 0.1));

        await Assert.That(ex.Message).Contains("close to");
    }

    [Test]
    public async Task toBeGreaterThan_WithNullExpected_ThrowsDescriptiveError()
    {
        var expectation = new Expectation<int?>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeGreaterThan(null));

        await Assert.That(ex.Message).Contains("cannot be null");
    }

    [Test]
    public async Task toBeLessThan_WithNullExpected_ThrowsDescriptiveError()
    {
        var expectation = new Expectation<int?>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeLessThan(null));

        await Assert.That(ex.Message).Contains("cannot be null");
    }

    [Test]
    public async Task toBeAtLeast_WithNullExpected_ThrowsDescriptiveError()
    {
        var expectation = new Expectation<int?>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeAtLeast(null));

        await Assert.That(ex.Message).Contains("cannot be null");
    }

    [Test]
    public async Task toBeAtMost_WithNullExpected_ThrowsDescriptiveError()
    {
        var expectation = new Expectation<int?>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeAtMost(null));

        await Assert.That(ex.Message).Contains("cannot be null");
    }

    [Test]
    public async Task toBeInRange_WithNullMin_ThrowsDescriptiveError()
    {
        var expectation = new Expectation<int?>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeInRange(null, 10));

        await Assert.That(ex.Message).Contains("cannot be null");
    }

    [Test]
    public async Task toBeInRange_WithNullMax_ThrowsDescriptiveError()
    {
        var expectation = new Expectation<int?>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeInRange(1, null));

        await Assert.That(ex.Message).Contains("cannot be null");
    }

    #endregion

    #region Negation (not)

    [Test]
    public async Task not_toBe_WhenDifferent_Passes()
    {
        var expectation = new Expectation<int>(42, "value");
        expectation.not.toBe(99); // Should pass - 42 is not 99
    }

    [Test]
    public async Task not_toBe_WhenEqual_Throws()
    {
        var expectation = new Expectation<int>(42, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBe(42));

        await Assert.That(ex.Message).Contains("to not be 42");
    }

    [Test]
    public async Task not_toBeNull_WhenNotNull_Passes()
    {
        var expectation = new Expectation<string?>("hello", "value");
        expectation.not.toBeNull(); // Should pass - "hello" is not null
    }

    [Test]
    public async Task not_toBeNull_WhenNull_Throws()
    {
        var expectation = new Expectation<string?>(null, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeNull());

        await Assert.That(ex.Message).Contains("to not be null");
    }

    [Test]
    public async Task not_toBeGreaterThan_WhenNotGreater_Passes()
    {
        var expectation = new Expectation<int>(5, "value");
        expectation.not.toBeGreaterThan(10); // Should pass - 5 is not greater than 10
    }

    [Test]
    public async Task not_toBeGreaterThan_WhenGreater_Throws()
    {
        var expectation = new Expectation<int>(15, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeGreaterThan(10));

        await Assert.That(ex.Message).Contains("to not be greater than 10");
    }

    [Test]
    public async Task not_toBeLessThan_WhenNotLess_Passes()
    {
        var expectation = new Expectation<int>(15, "value");
        expectation.not.toBeLessThan(10); // Should pass - 15 is not less than 10
    }

    [Test]
    public async Task not_toBeLessThan_WhenLess_Throws()
    {
        var expectation = new Expectation<int>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeLessThan(10));

        await Assert.That(ex.Message).Contains("to not be less than 10");
    }

    [Test]
    public async Task not_toBeInRange_WhenOutsideRange_Passes()
    {
        var expectation = new Expectation<int>(15, "value");
        expectation.not.toBeInRange(1, 10); // Should pass - 15 is not in [1, 10]
    }

    [Test]
    public async Task not_toBeInRange_WhenInRange_Throws()
    {
        var expectation = new Expectation<int>(5, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeInRange(1, 10));

        await Assert.That(ex.Message).Contains("to not be in range");
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
    public async Task toBeInstanceOf_WithWrongType_Throws()
    {
        var expectation = new Expectation<object>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeInstanceOf<int>());

        await Assert.That(ex.Message).Contains("to be instance of Int32");
        await Assert.That(ex.Message).Contains("but was String");
    }

    [Test]
    public async Task toBeInstanceOf_WithNullValue_Throws()
    {
        var expectation = new Expectation<object?>(null, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeInstanceOf<string>());

        await Assert.That(ex.Message).Contains("to be instance of String");
        await Assert.That(ex.Message).Contains("but was null");
    }

    [Test]
    public async Task not_toBeInstanceOf_WithWrongType_Passes()
    {
        var expectation = new Expectation<object>("hello", "value");
        expectation.not.toBeInstanceOf<int>(); // Should pass - string is not int
    }

    [Test]
    public async Task not_toBeInstanceOf_WithCorrectType_Throws()
    {
        var expectation = new Expectation<object>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeInstanceOf<string>());

        await Assert.That(ex.Message).Contains("to not be instance of String");
    }

    #endregion

    #region toBeEquivalentTo

    [Test]
    public async Task toBeEquivalentTo_WithEquivalentObjects_Passes()
    {
        var obj1 = new { Name = "Test", Value = 42 };
        var obj2 = new { Name = "Test", Value = 42 };
        var expectation = new Expectation<object>(obj1, "value");

        expectation.toBeEquivalentTo(obj2); // Should pass - same structure
    }

    [Test]
    public async Task toBeEquivalentTo_WithDifferentObjects_Throws()
    {
        var obj1 = new { Name = "Test", Value = 42 };
        var obj2 = new { Name = "Different", Value = 99 };
        var expectation = new Expectation<object>(obj1, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeEquivalentTo(obj2));

        await Assert.That(ex.Message).Contains("to be equivalent to");
    }

    [Test]
    public async Task toBeEquivalentTo_BothNull_Passes()
    {
        var expectation = new Expectation<object?>(null, "value");
        expectation.toBeEquivalentTo(null); // Should pass
    }

    [Test]
    public async Task toBeEquivalentTo_ActualNull_Throws()
    {
        var expectation = new Expectation<object?>(null, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeEquivalentTo(new { Name = "Test" }));

        await Assert.That(ex.Message).Contains("but was null");
    }

    [Test]
    public async Task toBeEquivalentTo_ExpectedNull_Throws()
    {
        var expectation = new Expectation<object?>(new { Name = "Test" }, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeEquivalentTo(null));

        await Assert.That(ex.Message).Contains("to be equivalent to null");
    }

    [Test]
    public async Task not_toBeEquivalentTo_WithDifferentObjects_Passes()
    {
        var obj1 = new { Name = "Test", Value = 42 };
        var obj2 = new { Name = "Different", Value = 99 };
        var expectation = new Expectation<object>(obj1, "value");

        expectation.not.toBeEquivalentTo(obj2); // Should pass - objects are different
    }

    [Test]
    public async Task not_toBeEquivalentTo_WithEquivalentObjects_Throws()
    {
        var obj1 = new { Name = "Test", Value = 42 };
        var obj2 = new { Name = "Test", Value = 42 };
        var expectation = new Expectation<object>(obj1, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeEquivalentTo(obj2));

        await Assert.That(ex.Message).Contains("to not be equivalent to");
    }

    [Test]
    public async Task not_toBeEquivalentTo_BothNull_Throws()
    {
        var expectation = new Expectation<object?>(null, "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.not.toBeEquivalentTo(null));

        await Assert.That(ex.Message).Contains("to not be equivalent to null");
    }

    [Test]
    public async Task not_toBeEquivalentTo_OneNull_Passes()
    {
        var expectation = new Expectation<object?>(new { Name = "Test" }, "value");
        expectation.not.toBeEquivalentTo(null); // Should pass - object is not equivalent to null
    }

    #endregion

    #region Null Edge Cases for Comparison

    [Test]
    public async Task toBeGreaterThan_NullExpected_Throws()
    {
        var expectation = new Expectation<string?>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeGreaterThan(null));

        await Assert.That(ex.Message).Contains("Expected value cannot be null");
    }

    [Test]
    public async Task toBeLessThan_NullExpected_Throws()
    {
        var expectation = new Expectation<string?>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeLessThan(null));

        await Assert.That(ex.Message).Contains("Expected value cannot be null");
    }

    [Test]
    public async Task toBeAtLeast_NullExpected_Throws()
    {
        var expectation = new Expectation<string?>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeAtLeast(null));

        await Assert.That(ex.Message).Contains("Expected value cannot be null");
    }

    [Test]
    public async Task toBeAtMost_NullExpected_Throws()
    {
        var expectation = new Expectation<string?>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeAtMost(null));

        await Assert.That(ex.Message).Contains("Expected value cannot be null");
    }

    [Test]
    public async Task toBeInRange_NullMin_Throws()
    {
        var expectation = new Expectation<string?>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeInRange(null, "z"));

        await Assert.That(ex.Message).Contains("Range bounds cannot be null");
    }

    [Test]
    public async Task toBeInRange_NullMax_Throws()
    {
        var expectation = new Expectation<string?>("hello", "value");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeInRange("a", null));

        await Assert.That(ex.Message).Contains("Range bounds cannot be null");
    }

    #endregion
}

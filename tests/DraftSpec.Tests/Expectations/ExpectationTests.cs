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

    #endregion
}
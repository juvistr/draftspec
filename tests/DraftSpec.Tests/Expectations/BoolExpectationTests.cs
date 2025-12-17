namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Tests for BoolExpectation assertions.
/// </summary>
public class BoolExpectationTests
{
    #region toBeTrue

    [Test]
    public async Task toBeTrue_WithTrue_Passes()
    {
        var expectation = new BoolExpectation(true, "flag");
        expectation.toBeTrue();
    }

    [Test]
    public async Task toBeTrue_WithFalse_Throws()
    {
        var expectation = new BoolExpectation(false, "flag");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeTrue());

        await Assert.That(ex.Message).Contains("to be true");
        await Assert.That(ex.Message).Contains("but was false");
    }

    #endregion

    #region toBeFalse

    [Test]
    public async Task toBeFalse_WithFalse_Passes()
    {
        var expectation = new BoolExpectation(false, "flag");
        expectation.toBeFalse();
    }

    [Test]
    public async Task toBeFalse_WithTrue_Throws()
    {
        var expectation = new BoolExpectation(true, "flag");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBeFalse());

        await Assert.That(ex.Message).Contains("to be false");
        await Assert.That(ex.Message).Contains("but was true");
    }

    #endregion

    #region toBe

    [Test]
    public async Task toBe_WithMatchingTrue_Passes()
    {
        var expectation = new BoolExpectation(true, "flag");
        expectation.toBe(true);
    }

    [Test]
    public async Task toBe_WithMatchingFalse_Passes()
    {
        var expectation = new BoolExpectation(false, "flag");
        expectation.toBe(false);
    }

    [Test]
    public async Task toBe_WithMismatch_Throws()
    {
        var expectation = new BoolExpectation(true, "flag");

        var ex = Assert.Throws<AssertionException>(() => expectation.toBe(false));

        await Assert.That(ex.Message).Contains("to be False");
        await Assert.That(ex.Message).Contains("but was True");
    }

    #endregion
}
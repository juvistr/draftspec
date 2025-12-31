namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Tests for ActionExpectation (exception testing).
/// </summary>
public class ActionExpectationTests
{
    #region toThrow<T>

    [Test]
    public async Task toThrow_WithCorrectExceptionType_ReturnsException()
    {
        var expectation = new ActionExpectation(
            () => throw new InvalidOperationException("test message"),
            "action");

        var ex = expectation.toThrow<InvalidOperationException>();

        await Assert.That(ex.Message).IsEqualTo("test message");
    }

    [Test]
    public async Task toThrow_WithWrongExceptionType_Throws()
    {
        var expectation = new ActionExpectation(
            () => throw new ArgumentException("wrong type"),
            "action");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.toThrow<InvalidOperationException>());

        await Assert.That(ex.Message).Contains("to throw InvalidOperationException");
        await Assert.That(ex.Message).Contains("but threw ArgumentException");
    }

    [Test]
    public async Task toThrow_WithNoException_Throws()
    {
        var expectation = new ActionExpectation(
            () =>
            {
                /* does nothing */
            },
            "action");

        var ex = Assert.Throws<AssertionException>(() =>
            expectation.toThrow<InvalidOperationException>());

        await Assert.That(ex.Message).Contains("to throw InvalidOperationException");
        await Assert.That(ex.Message).Contains("no exception was thrown");
    }

    [Test]
    public async Task toThrow_WithDerivedExceptionType_Catches()
    {
        var expectation = new ActionExpectation(
            () => throw new ArgumentNullException("param"),
            "action");

        // ArgumentNullException derives from ArgumentException
        var ex = expectation.toThrow<ArgumentException>();

        await Assert.That(ex).IsTypeOf<ArgumentNullException>();
    }

    #endregion

    #region toThrow (any exception)

    [Test]
    public async Task toThrow_AnyException_WhenThrows_ReturnsException()
    {
        var expectation = new ActionExpectation(
            () => throw new Exception("any exception"),
            "action");

        var ex = expectation.toThrow();

        await Assert.That(ex.Message).IsEqualTo("any exception");
    }

    [Test]
    public async Task toThrow_AnyException_WhenNoThrow_Throws()
    {
        var expectation = new ActionExpectation(
            () =>
            {
                /* does nothing */
            },
            "action");

        var ex = Assert.Throws<AssertionException>(() => expectation.toThrow());

        await Assert.That(ex.Message).Contains("to throw an exception");
        await Assert.That(ex.Message).Contains("no exception was thrown");
    }

    #endregion

    #region toNotThrow

    [Test]
    public async Task toNotThrow_WhenNoException_Passes()
    {
        var expectation = new ActionExpectation(
            () =>
            {
                /* does nothing */
            },
            "action");

        expectation.toNotThrow(); // Should not throw
    }

    [Test]
    public async Task toNotThrow_WhenThrows_ThrowsAssertionException()
    {
        var expectation = new ActionExpectation(
            () => throw new InvalidOperationException("oops"),
            "action");

        var ex = Assert.Throws<AssertionException>(() => expectation.toNotThrow());

        await Assert.That(ex.Message).Contains("to not throw");
        await Assert.That(ex.Message).Contains("InvalidOperationException");
        await Assert.That(ex.Message).Contains("oops");
    }

    #endregion
}

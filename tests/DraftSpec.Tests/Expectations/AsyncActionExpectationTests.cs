namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Tests for AsyncActionExpectation (async exception testing).
/// </summary>
public class AsyncActionExpectationTests
{
    #region toThrowAsync<T>

    [Test]
    public async Task toThrowAsync_WithCorrectExceptionType_ReturnsException()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("test message");
            },
            "asyncAction");

        var ex = await expectation.toThrowAsync<InvalidOperationException>();

        await Assert.That(ex.Message).IsEqualTo("test message");
    }

    [Test]
    public async Task toThrowAsync_WithWrongExceptionType_Throws()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                throw new ArgumentException("wrong type");
            },
            "asyncAction");

        var ex = await Assert.ThrowsAsync<AssertionException>(async () =>
            await expectation.toThrowAsync<InvalidOperationException>());

        await Assert.That(ex.Message).Contains("to throw InvalidOperationException");
        await Assert.That(ex.Message).Contains("but threw ArgumentException");
    }

    [Test]
    public async Task toThrowAsync_WithNoException_Throws()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                // does nothing
            },
            "asyncAction");

        var ex = await Assert.ThrowsAsync<AssertionException>(async () =>
            await expectation.toThrowAsync<InvalidOperationException>());

        await Assert.That(ex.Message).Contains("to throw InvalidOperationException");
        await Assert.That(ex.Message).Contains("no exception was thrown");
    }

    [Test]
    public async Task toThrowAsync_WithDerivedExceptionType_Catches()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                throw new ArgumentNullException("param");
            },
            "asyncAction");

        // ArgumentNullException derives from ArgumentException
        var ex = await expectation.toThrowAsync<ArgumentException>();

        await Assert.That(ex).IsTypeOf<ArgumentNullException>();
    }

    [Test]
    public async Task toThrowAsync_WithImmediateThrow_Catches()
    {
        // Test that sync exceptions in async methods are caught
        var expectation = new AsyncActionExpectation(
            () => throw new InvalidOperationException("sync throw"),
            "asyncAction");

        var ex = await expectation.toThrowAsync<InvalidOperationException>();

        await Assert.That(ex.Message).IsEqualTo("sync throw");
    }

    #endregion

    #region toThrowAsync (any exception)

    [Test]
    public async Task toThrowAsync_AnyException_WhenThrows_ReturnsException()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                throw new Exception("any exception");
            },
            "asyncAction");

        var ex = await expectation.toThrowAsync();

        await Assert.That(ex.Message).IsEqualTo("any exception");
    }

    [Test]
    public async Task toThrowAsync_AnyException_WhenNoThrow_Throws()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                // does nothing
            },
            "asyncAction");

        var ex = await Assert.ThrowsAsync<AssertionException>(async () =>
            await expectation.toThrowAsync());

        await Assert.That(ex.Message).Contains("to throw an exception");
        await Assert.That(ex.Message).Contains("no exception was thrown");
    }

    #endregion

    #region toNotThrowAsync

    [Test]
    public async Task toNotThrowAsync_WhenNoException_Passes()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                // does nothing
            },
            "asyncAction");

        await expectation.toNotThrowAsync(); // Should not throw
    }

    [Test]
    public async Task toNotThrowAsync_WhenThrows_ThrowsAssertionException()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("oops");
            },
            "asyncAction");

        var ex = await Assert.ThrowsAsync<AssertionException>(async () =>
            await expectation.toNotThrowAsync());

        await Assert.That(ex.Message).Contains("to not throw");
        await Assert.That(ex.Message).Contains("InvalidOperationException");
        await Assert.That(ex.Message).Contains("oops");
    }

    [Test]
    public async Task toNotThrowAsync_WithDelayedCompletion_Passes()
    {
        var expectation = new AsyncActionExpectation(
            async () =>
            {
                await Task.Delay(10);
            },
            "asyncAction");

        await expectation.toNotThrowAsync(); // Should not throw
    }

    #endregion

    #region DSL Integration

    [Test]
    public async Task expect_WithFuncTask_ReturnsAsyncActionExpectation()
    {
        // Test that the DSL correctly creates AsyncActionExpectation
        var expectation = DraftSpec.Dsl.expect(async () => await Task.Yield());

        await Assert.That(expectation).IsTypeOf<AsyncActionExpectation>();
    }

    [Test]
    public async Task expect_WithAsyncLambda_CapturesExpression()
    {
        var expectation = DraftSpec.Dsl.expect(async () => await Task.Yield());

        // The expression should be captured (non-null)
        await Assert.That(expectation.Expression).IsNotNull();
    }

    #endregion
}

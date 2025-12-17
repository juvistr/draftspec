namespace DraftSpec.Tests.Expectations;

/// <summary>
/// Example custom matchers demonstrating the extension pattern.
/// </summary>
public static class CustomMatchers
{
    /// <summary>
    /// Custom DateTime matcher: asserts the date is after another date.
    /// </summary>
    public static void toBeAfter(this Expectation<DateTime> exp, DateTime other)
    {
        if (exp.Actual <= other)
            throw new AssertionException(
                $"Expected {exp.Expression} to be after {other:O}, but was {exp.Actual:O}");
    }

    /// <summary>
    /// Custom DateTime matcher: asserts the date is before another date.
    /// </summary>
    public static void toBeBefore(this Expectation<DateTime> exp, DateTime other)
    {
        if (exp.Actual >= other)
            throw new AssertionException(
                $"Expected {exp.Expression} to be before {other:O}, but was {exp.Actual:O}");
    }

    /// <summary>
    /// Custom string matcher: asserts the string is a valid email format.
    /// </summary>
    public static void toBeValidEmail(this StringExpectation exp)
    {
        if (exp.Actual is null || !exp.Actual.Contains('@') || !exp.Actual.Contains('.'))
            throw new AssertionException(
                $"Expected {exp.Expression} to be a valid email, but was \"{exp.Actual}\"");
    }

    /// <summary>
    /// Custom collection matcher: asserts all items satisfy a predicate.
    /// </summary>
    public static void toAllSatisfy<T>(this CollectionExpectation<T> exp, Func<T, bool> predicate, string description)
    {
        var failing = exp.Actual.Where(x => !predicate(x)).ToList();
        if (failing.Count > 0)
            throw new AssertionException(
                $"Expected all items in {exp.Expression} to {description}, but {failing.Count} item(s) failed");
    }
}

/// <summary>
/// Tests verifying that custom matcher extension methods work with the expectation API.
/// These tests demonstrate the extensibility pattern for domain-specific matchers.
/// </summary>
public class ExtensionTests
{
    #region Extension Method Access Tests

    [Test]
    public async Task Expectation_ExposesActualValue()
    {
        var exp = new Expectation<int>(42, "myValue");

        await Assert.That(exp.Actual).IsEqualTo(42);
    }

    [Test]
    public async Task Expectation_ExposesExpression()
    {
        var exp = new Expectation<int>(42, "myValue");

        await Assert.That(exp.Expression).IsEqualTo("myValue");
    }

    [Test]
    public async Task StringExpectation_ExposesActualValue()
    {
        var exp = new StringExpectation("hello", "myString");

        await Assert.That(exp.Actual).IsEqualTo("hello");
    }

    [Test]
    public async Task StringExpectation_ExposesExpression()
    {
        var exp = new StringExpectation("hello", "myString");

        await Assert.That(exp.Expression).IsEqualTo("myString");
    }

    [Test]
    public async Task BoolExpectation_ExposesActualValue()
    {
        var exp = new BoolExpectation(true, "myBool");

        await Assert.That(exp.Actual).IsEqualTo(true);
    }

    [Test]
    public async Task ActionExpectation_ExposesAction()
    {
        var executed = false;
        var exp = new ActionExpectation(() => executed = true, "myAction");

        exp.Action();

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task CollectionExpectation_ExposesActualValue()
    {
        var items = new[] { 1, 2, 3 };
        var exp = new CollectionExpectation<int>(items, "myCollection");

        await Assert.That(exp.Actual.Count()).IsEqualTo(3);
    }

    #endregion

    #region Custom DateTime Matcher Tests

    [Test]
    public async Task ToBeAfter_WhenDateIsAfter_Passes()
    {
        var later = new DateTime(2025, 12, 15);
        var earlier = new DateTime(2025, 1, 1);

        var exp = new Expectation<DateTime>(later, "orderDate");

        // Should not throw
        exp.toBeAfter(earlier);

        await Assert.That(true).IsTrue(); // Test passed
    }

    [Test]
    public async Task ToBeAfter_WhenDateIsBefore_ThrowsAssertionException()
    {
        var earlier = new DateTime(2025, 1, 1);
        var later = new DateTime(2025, 12, 15);

        var exp = new Expectation<DateTime>(earlier, "orderDate");

        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
        {
            exp.toBeAfter(later);
            return Task.CompletedTask;
        });

        await Assert.That(exception.Message).Contains("orderDate");
        await Assert.That(exception.Message).Contains("to be after");
    }

    [Test]
    public async Task ToBeBefore_WhenDateIsBefore_Passes()
    {
        var earlier = new DateTime(2025, 1, 1);
        var later = new DateTime(2025, 12, 15);

        var exp = new Expectation<DateTime>(earlier, "deadline");

        // Should not throw
        exp.toBeBefore(later);

        await Assert.That(true).IsTrue(); // Test passed
    }

    #endregion

    #region Custom String Matcher Tests

    [Test]
    public async Task ToBeValidEmail_WhenValidEmail_Passes()
    {
        var exp = new StringExpectation("user@example.com", "email");

        // Should not throw
        exp.toBeValidEmail();

        await Assert.That(true).IsTrue(); // Test passed
    }

    [Test]
    public async Task ToBeValidEmail_WhenInvalidEmail_ThrowsAssertionException()
    {
        var exp = new StringExpectation("not-an-email", "email");

        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
        {
            exp.toBeValidEmail();
            return Task.CompletedTask;
        });

        await Assert.That(exception.Message).Contains("email");
        await Assert.That(exception.Message).Contains("valid email");
    }

    #endregion

    #region Custom Collection Matcher Tests

    [Test]
    public async Task ToAllSatisfy_WhenAllMatch_Passes()
    {
        var numbers = new[] { 2, 4, 6, 8 };
        var exp = new CollectionExpectation<int>(numbers, "evenNumbers");

        // Should not throw
        exp.toAllSatisfy(n => n % 2 == 0, "be even");

        await Assert.That(true).IsTrue(); // Test passed
    }

    [Test]
    public async Task ToAllSatisfy_WhenSomeFail_ThrowsAssertionException()
    {
        var numbers = new[] { 2, 3, 4, 5 };
        var exp = new CollectionExpectation<int>(numbers, "numbers");

        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
        {
            exp.toAllSatisfy(n => n % 2 == 0, "be even");
            return Task.CompletedTask;
        });

        await Assert.That(exception.Message).Contains("numbers");
        await Assert.That(exception.Message).Contains("2 item(s) failed");
    }

    #endregion

    #region Expression Capture Integration

    [Test]
    public async Task ExtensionMethod_PreservesExpressionInErrorMessage()
    {
        var testDate = new DateTime(2025, 1, 1);
        var exp = new Expectation<DateTime>(testDate, "order.ShippedDate");

        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
        {
            exp.toBeAfter(new DateTime(2025, 12, 31));
            return Task.CompletedTask;
        });

        // The expression "order.ShippedDate" should appear in the error message
        await Assert.That(exception.Message).Contains("order.ShippedDate");
    }

    #endregion
}
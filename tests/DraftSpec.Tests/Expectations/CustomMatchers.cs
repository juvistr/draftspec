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

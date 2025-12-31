namespace DraftSpec;

/// <summary>
/// Exception thrown when an expectation assertion fails.
/// </summary>
/// <remarks>
/// This exception is thrown by the expect() API when an assertion does not match.
/// The message contains the expected vs actual values and the expression that failed.
/// </remarks>
/// <example>
/// <code>
/// // This will throw AssertionException with message:
/// // "Expected 1 + 1 to be 3, but was 2"
/// expect(1 + 1).toBe(3);
/// </code>
/// </example>
public class AssertionException : Exception
{
    /// <summary>
    /// Creates a new assertion exception with the specified message.
    /// </summary>
    /// <param name="message">A message describing what was expected vs what was found.</param>
    public AssertionException(string message) : base(message)
    {
    }
}

namespace DraftSpec;

/// <summary>
/// Exception thrown when a spec execution fails.
/// </summary>
public class SpecExecutionException : Exception
{
    /// <summary>
    /// Creates a new SpecExecutionException with the specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SpecExecutionException(string message) : base(message) { }

    /// <summary>
    /// Creates a new SpecExecutionException with the specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SpecExecutionException(string message, Exception innerException) : base(message, innerException) { }
}

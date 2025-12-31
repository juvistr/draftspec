namespace DraftSpec;

/// <summary>
/// Exception thrown when a spec file has compilation errors.
/// </summary>
public class CompilationException : Exception
{
    /// <summary>
    /// Creates a new CompilationException with the specified error message.
    /// </summary>
    /// <param name="message">The compilation error message.</param>
    public CompilationException(string message) : base(message) { }

    /// <summary>
    /// Creates a new CompilationException with the specified error message and inner exception.
    /// </summary>
    /// <param name="message">The compilation error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CompilationException(string message, Exception innerException) : base(message, innerException) { }
}

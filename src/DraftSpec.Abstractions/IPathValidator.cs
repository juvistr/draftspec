namespace DraftSpec.Abstractions;

/// <summary>
/// Validates paths to prevent path traversal attacks.
/// </summary>
public interface IPathValidator
{
    /// <summary>
    /// Validates that a path is within the specified base directory.
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <param name="baseDirectory">The base directory that path must be within. Defaults to current directory.</param>
    /// <exception cref="System.Security.SecurityException">Thrown when path attempts to escape base directory</exception>
    void ValidatePathWithinBase(string path, string? baseDirectory = null);

    /// <summary>
    /// Attempts to validate that a path is within the specified base directory.
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <param name="baseDirectory">The base directory that path must be within. Defaults to current directory.</param>
    /// <param name="errorMessage">The error message if validation fails, null if valid</param>
    /// <returns>True if valid, false otherwise</returns>
    bool TryValidatePathWithinBase(string path, string? baseDirectory, out string? errorMessage);

    /// <summary>
    /// Validates that a filename does not contain path separators or other dangerous characters.
    /// </summary>
    /// <param name="name">The filename to validate</param>
    /// <exception cref="ArgumentException">Thrown when name contains invalid characters</exception>
    void ValidateFileName(string name);

    /// <summary>
    /// Attempts to validate a filename without throwing an exception.
    /// </summary>
    /// <param name="name">The filename to validate</param>
    /// <param name="errorMessage">The error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    bool TryValidateFileName(string name, out string? errorMessage);
}

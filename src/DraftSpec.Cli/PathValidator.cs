using System.Security;

namespace DraftSpec.Cli;

/// <summary>
/// Validates paths to prevent path traversal attacks.
/// </summary>
public static class PathValidator
{
    /// <summary>
    /// Validates that a path is within the specified base directory.
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <param name="baseDirectory">The base directory that path must be within. Defaults to current directory.</param>
    /// <exception cref="SecurityException">Thrown when path attempts to escape base directory</exception>
    public static void ValidatePathWithinBase(string path, string? baseDirectory = null)
    {
        var basePath = Path.GetFullPath(baseDirectory ?? Directory.GetCurrentDirectory());
        var fullPath = Path.GetFullPath(path);

        // Security: Add trailing separator to prevent prefix bypass attacks
        // e.g., "/var/app/specs-evil" should NOT pass check for base "/var/app/specs"
        var normalizedBase = basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        // Use platform-appropriate case sensitivity
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!normalizedPath.StartsWith(normalizedBase, comparison))
            // Generic error message to avoid leaking internal paths
            throw new SecurityException("Path must be within the working directory");
    }

    /// <summary>
    /// Validates that a filename does not contain path separators or other dangerous characters.
    /// </summary>
    /// <param name="name">The filename to validate</param>
    /// <exception cref="ArgumentException">Thrown when name contains invalid characters</exception>
    public static void ValidateFileName(string name)
    {
        if (!TryValidateFileName(name, out var error))
            throw new ArgumentException(error, nameof(name));
    }

    /// <summary>
    /// Attempts to validate a filename without throwing an exception.
    /// </summary>
    /// <param name="name">The filename to validate</param>
    /// <param name="error">The error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool TryValidateFileName(string name, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Name cannot be empty";
            return false;
        }

        // Check for path separators (both Windows and Unix)
        if (name.Contains(Path.DirectorySeparatorChar) ||
            name.Contains(Path.AltDirectorySeparatorChar) ||
            name.Contains('/') ||
            name.Contains('\\'))
        {
            error = "Name cannot contain path separators. Use a simple name without directories.";
            return false;
        }

        // Check for parent directory traversal
        if (name is ".." or "." || name.StartsWith("..", StringComparison.Ordinal))
        {
            error = "Name cannot be a relative path reference";
            return false;
        }

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            error = "Name contains invalid characters";
            return false;
        }

        return true;
    }
}

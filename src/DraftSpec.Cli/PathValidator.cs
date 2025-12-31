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
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        // Check for path separators (both Windows and Unix)
        if (name.Contains(Path.DirectorySeparatorChar) ||
            name.Contains(Path.AltDirectorySeparatorChar) ||
            name.Contains('/') ||
            name.Contains('\\'))
        {
            throw new ArgumentException(
                "Name cannot contain path separators. Use a simple name without directories.",
                nameof(name));
        }

        // Check for parent directory traversal
        if (name == ".." || name == "." || name.StartsWith("..", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Name cannot be a relative path reference",
                nameof(name));
        }

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            throw new ArgumentException(
                "Name contains invalid characters",
                nameof(name));
        }
    }
}

using System.Security;

namespace DraftSpec.Cli;

/// <summary>
/// Validates paths to prevent path traversal attacks.
/// </summary>
public sealed class PathValidator : IPathValidator
{
    private readonly IPathComparer _pathComparer;

    public PathValidator(IPathComparer pathComparer)
    {
        _pathComparer = pathComparer;
    }

    /// <inheritdoc />
    public void ValidatePathWithinBase(string path, string? baseDirectory = null)
    {
        if (!TryValidatePathWithinBase(path, baseDirectory, out var error))
            // Generic error message to avoid leaking internal paths
            throw new SecurityException(error);
    }

    /// <inheritdoc />
    public bool TryValidatePathWithinBase(string path, string? baseDirectory, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var basePath = Path.GetFullPath(baseDirectory ?? Directory.GetCurrentDirectory());
            var fullPath = Path.GetFullPath(path);

            // Security: Add trailing separator to prevent prefix bypass attacks
            // e.g., "/var/app/specs-evil" should NOT pass check for base "/var/app/specs"
            var normalizedBase = basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var normalizedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!normalizedPath.StartsWith(normalizedBase, _pathComparer.Comparison))
            {
                errorMessage = "Path must be within the working directory";
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            errorMessage = "Invalid path";
            return false;
        }
    }

    /// <inheritdoc />
    public void ValidateFileName(string name)
    {
        if (!TryValidateFileName(name, out var error))
            throw new ArgumentException(error, nameof(name));
    }

    /// <inheritdoc />
    public bool TryValidateFileName(string name, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "Name cannot be empty";
            return false;
        }

        // Check for path separators (both Windows and Unix)
        if (name.Contains(Path.DirectorySeparatorChar) ||
            name.Contains(Path.AltDirectorySeparatorChar) ||
            name.Contains('/') ||
            name.Contains('\\'))
        {
            errorMessage = "Name cannot contain path separators. Use a simple name without directories.";
            return false;
        }

        // Check for parent directory traversal
        if (name is ".." or "." || name.StartsWith("..", StringComparison.Ordinal))
        {
            errorMessage = "Name cannot be a relative path reference";
            return false;
        }

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            errorMessage = "Name contains invalid characters";
            return false;
        }

        return true;
    }
}

using System.Security;

namespace DraftSpec.Cli;

public class SpecFinder : ISpecFinder
{
    private readonly IFileSystem _fileSystem;
    private readonly IPathComparer _pathComparer;

    /// <summary>
    /// Creates a SpecFinder with the given file system and path comparer abstractions.
    /// </summary>
    public SpecFinder(IFileSystem fileSystem, IPathComparer pathComparer)
    {
        _fileSystem = fileSystem;
        _pathComparer = pathComparer;
    }

    /// <summary>
    /// Find spec files at the given path.
    /// </summary>
    /// <param name="path">File or directory path to search</param>
    /// <param name="baseDirectory">Base directory for path traversal validation. Defaults to current directory.</param>
    /// <returns>List of spec file paths</returns>
    /// <exception cref="SecurityException">Thrown when path attempts to escape base directory</exception>
    /// <exception cref="ArgumentException">Thrown when path is invalid or no specs found</exception>
    public IReadOnlyList<string> FindSpecs(string path, string? baseDirectory = null)
    {
        var basePath = Path.GetFullPath(baseDirectory ?? Directory.GetCurrentDirectory());
        var fullPath = Path.GetFullPath(path);

        // Security: Validate path is within base directory to prevent path traversal
        // IMPORTANT: Add trailing separator to prevent prefix bypass attacks
        // e.g., "/var/app/specs-evil" should NOT pass check for base "/var/app/specs"
        var normalizedBase = basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!normalizedPath.StartsWith(normalizedBase, _pathComparer.Comparison))
            // Generic error message to avoid leaking internal paths
            throw new SecurityException("Path must be within the base directory");

        if (_fileSystem.FileExists(fullPath))
        {
            if (!fullPath.EndsWith(".spec.csx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"File must end with .spec.csx: {path}");
            return [fullPath];
        }

        if (_fileSystem.DirectoryExists(fullPath))
        {
            return _fileSystem.GetFiles(fullPath, "*.spec.csx", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();
        }

        throw new ArgumentException($"Path not found: {path}");
    }
}

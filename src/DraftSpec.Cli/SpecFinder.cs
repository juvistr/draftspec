using System.Security;

namespace DraftSpec.Cli;

public class SpecFinder
{
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
        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException($"Path must be within the base directory: {path}");
        }

        if (File.Exists(fullPath))
        {
            if (!fullPath.EndsWith(".spec.csx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"File must end with .spec.csx: {path}");
            return [fullPath];
        }

        if (Directory.Exists(fullPath))
        {
            var specs = Directory.GetFiles(fullPath, "*.spec.csx", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();

            if (specs.Count == 0)
                throw new ArgumentException($"No *.spec.csx files found in: {path}");

            return specs;
        }

        throw new ArgumentException($"Path not found: {path}");
    }
}

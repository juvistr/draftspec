namespace DraftSpec.Cli;

/// <summary>
/// Finds spec files in a directory or at a specific path.
/// </summary>
public interface ISpecFinder
{
    /// <summary>
    /// Find spec files at the given path with optional base directory validation.
    /// </summary>
    /// <param name="path">File or directory path to search</param>
    /// <param name="baseDirectory">Base directory for path traversal validation (defaults to current directory)</param>
    /// <returns>List of spec file paths, sorted alphabetically</returns>
    /// <exception cref="System.Security.SecurityException">Path attempts to escape base directory</exception>
    /// <exception cref="ArgumentException">Path is invalid or no specs found</exception>
    IReadOnlyList<string> FindSpecs(string path, string? baseDirectory = null);
}
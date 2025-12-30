namespace DraftSpec.TestingPlatform;

/// <summary>
/// Abstraction for spec file discovery operations.
/// Enables unit testing of discovery logic without file system dependencies.
/// </summary>
public interface ISpecFileProvider
{
    /// <summary>
    /// Finds all spec files (*.spec.csx) in the given directory and subdirectories.
    /// </summary>
    /// <param name="directory">The directory to search.</param>
    /// <returns>Enumerable of absolute paths to spec files.</returns>
    IEnumerable<string> GetSpecFiles(string directory);

    /// <summary>
    /// Gets the relative path from a base directory to an absolute path.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="absolutePath">The absolute path to make relative.</param>
    /// <returns>The relative path.</returns>
    string GetRelativePath(string basePath, string absolutePath);

    /// <summary>
    /// Gets the full absolute path from a base directory and relative path.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="relativePath">The relative path to resolve.</param>
    /// <returns>The absolute path.</returns>
    string GetAbsolutePath(string basePath, string relativePath);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    bool FileExists(string path);
}

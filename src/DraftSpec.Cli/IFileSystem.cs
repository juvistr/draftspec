namespace DraftSpec.Cli;

/// <summary>
/// Abstraction over file system operations for testability.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Check if a file exists.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Write text to a file synchronously.
    /// </summary>
    void WriteAllText(string path, string content);

    /// <summary>
    /// Write text to a file asynchronously.
    /// </summary>
    Task WriteAllTextAsync(string path, string content, CancellationToken ct = default);

    /// <summary>
    /// Read all text from a file.
    /// </summary>
    string ReadAllText(string path);

    /// <summary>
    /// Check if a directory exists.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Create a directory and any parent directories.
    /// </summary>
    void CreateDirectory(string path);

    /// <summary>
    /// Get files in a directory matching a search pattern.
    /// </summary>
    string[] GetFiles(string path, string searchPattern);

    /// <summary>
    /// Get files in a directory matching a search pattern, with search option.
    /// </summary>
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Enumerate files in a directory matching a search pattern.
    /// </summary>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Enumerate directories matching a search pattern.
    /// </summary>
    IEnumerable<string> EnumerateDirectories(string path, string searchPattern);

    /// <summary>
    /// Get the last write time of a file in UTC.
    /// </summary>
    DateTime GetLastWriteTimeUtc(string path);
}

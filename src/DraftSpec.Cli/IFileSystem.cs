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

/// <summary>
/// Implementation that delegates to System.IO.
/// </summary>
public class FileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, content, ct);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string[] GetFiles(string path, string searchPattern)
        => Directory.GetFiles(path, searchPattern);

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory.GetFiles(path, searchPattern, searchOption);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory.EnumerateFiles(path, searchPattern, searchOption);

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
        => Directory.EnumerateDirectories(path, searchPattern);

    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
}

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
}

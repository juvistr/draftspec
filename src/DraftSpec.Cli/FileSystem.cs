namespace DraftSpec.Cli;

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

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
        => File.ReadAllTextAsync(path, ct);

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

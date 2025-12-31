using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// In-memory mock file system for unit testing.
/// Allows configuring which files and directories "exist" without touching the real file system.
/// Supports fluent builder API for easy test setup.
/// </summary>
public class MockFileSystem : IFileSystem
{
    private readonly HashSet<string> _files;
    private readonly HashSet<string> _directories;
    private readonly Dictionary<string, List<string>> _directoryFiles;
    private readonly Dictionary<string, string> _fileContents;
    private readonly Dictionary<string, DateTime> _lastWriteTimes;
    private readonly StringComparer _comparer;

    public MockFileSystem(bool caseSensitive = false)
    {
        _comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        _files = new(_comparer);
        _directories = new(_comparer);
        _directoryFiles = new(_comparer);
        _fileContents = new(_comparer);
        _lastWriteTimes = new(_comparer);
    }

    /// <summary>
    /// Gets all files that have been written to (for assertions).
    /// </summary>
    public IReadOnlyDictionary<string, string> WrittenFiles => _fileContents;

    /// <summary>
    /// Gets how many times CreateDirectory() was called.
    /// </summary>
    public int CreateDirectoryCalls { get; private set; }

    /// <summary>
    /// Add a file that will be reported as existing.
    /// </summary>
    public MockFileSystem AddFile(string path, string? content = null)
    {
        var fullPath = Path.GetFullPath(path);
        _files.Add(fullPath);
        if (content != null)
            _fileContents[fullPath] = content;
        return this;
    }

    /// <summary>
    /// Add a directory that will be reported as existing.
    /// </summary>
    public MockFileSystem AddDirectory(string path)
    {
        _directories.Add(Path.GetFullPath(path));
        return this;
    }

    /// <summary>
    /// Add files within a directory. The directory is automatically marked as existing.
    /// </summary>
    public MockFileSystem AddFilesInDirectory(string directory, params string[] fileNames)
    {
        var fullDir = Path.GetFullPath(directory);
        _directories.Add(fullDir);
        var files = fileNames.Select(f => Path.Combine(fullDir, f)).ToList();
        _directoryFiles[fullDir] = files;
        foreach (var file in files)
            _files.Add(file);
        return this;
    }

    /// <summary>
    /// Set the last write time for a file.
    /// </summary>
    public MockFileSystem SetLastWriteTime(string path, DateTime utcTime)
    {
        _lastWriteTimes[Path.GetFullPath(path)] = utcTime;
        return this;
    }

    public bool FileExists(string path) => _files.Contains(Path.GetFullPath(path));

    public bool DirectoryExists(string path) => _directories.Contains(Path.GetFullPath(path));

    public string[] GetFiles(string path, string searchPattern)
    {
        return GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
    }

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        var fullPath = Path.GetFullPath(path);
        if (_directoryFiles.TryGetValue(fullPath, out var files))
        {
            // Simple pattern matching for *.spec.csx style patterns
            if (searchPattern.StartsWith("*"))
            {
                var extension = searchPattern[1..]; // e.g., ".spec.csx"
                return files.Where(f => f.EndsWith(extension, StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            return files.ToArray();
        }
        return [];
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return GetFiles(path, searchPattern, searchOption);
    }

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
    {
        // Return subdirectories that match the pattern
        var fullPath = Path.GetFullPath(path);
        return _directories
            .Where(d => d.StartsWith(fullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        && d != fullPath)
            .Select(d =>
            {
                // Get immediate subdirectory
                var relativePath = d[(fullPath.Length + 1)..];
                var firstSep = relativePath.IndexOf(Path.DirectorySeparatorChar);
                return firstSep > 0
                    ? Path.Combine(fullPath, relativePath[..firstSep])
                    : d;
            })
            .Distinct();
    }

    public string ReadAllText(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return _fileContents.TryGetValue(fullPath, out var content) ? content : "";
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(ReadAllText(path));
    }

    public void WriteAllText(string path, string content)
    {
        var fullPath = Path.GetFullPath(path);
        _files.Add(fullPath);
        _fileContents[fullPath] = content;
    }

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
    {
        WriteAllText(path, content);
        return Task.CompletedTask;
    }

    public void CreateDirectory(string path)
    {
        CreateDirectoryCalls++;
        _directories.Add(Path.GetFullPath(path));
    }

    public DateTime GetLastWriteTimeUtc(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return _lastWriteTimes.TryGetValue(fullPath, out var time) ? time : DateTime.MinValue;
    }
}

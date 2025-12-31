using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Integration tests for FileSystem wrapper.
/// Uses actual file system operations with temp directories.
/// </summary>
public class FileSystemTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystem _fileSystem;

    public FileSystemTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _fileSystem = new FileSystem();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #region File Operations

    [Test]
    public async Task FileExists_ExistingFile_ReturnsTrue()
    {
        var path = Path.Combine(_tempDir, "existing.txt");
        File.WriteAllText(path, "content");

        var result = _fileSystem.FileExists(path);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task FileExists_NonExistingFile_ReturnsFalse()
    {
        var path = Path.Combine(_tempDir, "nonexistent.txt");

        var result = _fileSystem.FileExists(path);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ReadAllText_ExistingFile_ReturnsContent()
    {
        var path = Path.Combine(_tempDir, "read.txt");
        File.WriteAllText(path, "test content");

        var result = _fileSystem.ReadAllText(path);

        await Assert.That(result).IsEqualTo("test content");
    }

    [Test]
    public async Task WriteAllText_CreatesFileWithContent()
    {
        var path = Path.Combine(_tempDir, "write.txt");

        _fileSystem.WriteAllText(path, "written content");

        await Assert.That(File.Exists(path)).IsTrue();
        await Assert.That(File.ReadAllText(path)).IsEqualTo("written content");
    }

    [Test]
    public async Task WriteAllTextAsync_CreatesFileWithContent()
    {
        var path = Path.Combine(_tempDir, "write-async.txt");

        await _fileSystem.WriteAllTextAsync(path, "async content");

        await Assert.That(File.Exists(path)).IsTrue();
        await Assert.That(File.ReadAllText(path)).IsEqualTo("async content");
    }

    [Test]
    public async Task GetLastWriteTimeUtc_ReturnsValidTime()
    {
        var path = Path.Combine(_tempDir, "time.txt");
        File.WriteAllText(path, "content");
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = _fileSystem.GetLastWriteTimeUtc(path);

        await Assert.That(result).IsGreaterThan(before.AddMinutes(-1));
        await Assert.That(result).IsLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Directory Operations

    [Test]
    public async Task DirectoryExists_ExistingDirectory_ReturnsTrue()
    {
        var result = _fileSystem.DirectoryExists(_tempDir);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DirectoryExists_NonExistingDirectory_ReturnsFalse()
    {
        var path = Path.Combine(_tempDir, "nonexistent");

        var result = _fileSystem.DirectoryExists(path);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CreateDirectory_CreatesDirectory()
    {
        var path = Path.Combine(_tempDir, "newdir");

        _fileSystem.CreateDirectory(path);

        await Assert.That(Directory.Exists(path)).IsTrue();
    }

    [Test]
    public async Task GetFiles_WithPattern_ReturnsMatchingFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "");
        File.WriteAllText(Path.Combine(_tempDir, "file3.md"), "");

        var result = _fileSystem.GetFiles(_tempDir, "*.txt");

        await Assert.That(result.Length).IsEqualTo(2);
    }

    [Test]
    public async Task GetFiles_WithSearchOption_SearchesRecursively()
    {
        var subdir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subdir);
        File.WriteAllText(Path.Combine(_tempDir, "root.txt"), "");
        File.WriteAllText(Path.Combine(subdir, "nested.txt"), "");

        var result = _fileSystem.GetFiles(_tempDir, "*.txt", SearchOption.AllDirectories);

        await Assert.That(result.Length).IsEqualTo(2);
    }

    [Test]
    public async Task EnumerateFiles_WithSearchOption_EnumeratesRecursively()
    {
        var subdir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subdir);
        File.WriteAllText(Path.Combine(_tempDir, "root.txt"), "");
        File.WriteAllText(Path.Combine(subdir, "nested.txt"), "");

        var result = _fileSystem.EnumerateFiles(_tempDir, "*.txt", SearchOption.AllDirectories).ToList();

        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task EnumerateDirectories_WithPattern_ReturnsMatchingDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "dir1"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "dir2"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "other"));

        var result = _fileSystem.EnumerateDirectories(_tempDir, "dir*").ToList();

        await Assert.That(result.Count).IsEqualTo(2);
    }

    #endregion
}

using DraftSpec.Mcp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DraftSpec.Tests.Mcp.Services;

/// <summary>
/// Tests for TempFileManager.
/// </summary>
[NotInParallel]
public class TempFileManagerTests
{
    private TempFileManager _manager = null!;

    [Before(Test)]
    public void SetUp()
    {
        var logger = NullLogger<TempFileManager>.Instance;
        _manager = new TempFileManager(logger);
    }

    [After(Test)]
    public void TearDown()
    {
        // Clean up temp directory contents we created
        // Don't delete the directory itself as other tests may use it
    }

    #region TempDirectory

    [Test]
    public async Task TempDirectory_ReturnsPath()
    {
        var tempDir = _manager.TempDirectory;

        await Assert.That(tempDir).IsNotNull();
        await Assert.That(tempDir).IsNotEmpty();
    }

    [Test]
    public async Task TempDirectory_PathExists()
    {
        var tempDir = _manager.TempDirectory;

        await Assert.That(Directory.Exists(tempDir)).IsTrue();
    }

    [Test]
    public async Task TempDirectory_PathContainsDraftspec()
    {
        var tempDir = _manager.TempDirectory;

        await Assert.That(tempDir).Contains("draftspec");
    }

    #endregion

    #region CreateTempSpecFileAsync

    [Test]
    public async Task CreateTempSpecFileAsync_CreatesFile()
    {
        var content = "describe('test', () => {});";

        var path = await _manager.CreateTempSpecFileAsync(content, CancellationToken.None);

        try
        {
            await Assert.That(File.Exists(path)).IsTrue();
        }
        finally
        {
            _manager.Cleanup(path);
        }
    }

    [Test]
    public async Task CreateTempSpecFileAsync_FileContainsContent()
    {
        var content = "describe('test', () => { it('works'); });";

        var path = await _manager.CreateTempSpecFileAsync(content, CancellationToken.None);

        try
        {
            var fileContent = await File.ReadAllTextAsync(path);
            await Assert.That(fileContent).IsEqualTo(content);
        }
        finally
        {
            _manager.Cleanup(path);
        }
    }

    [Test]
    public async Task CreateTempSpecFileAsync_CreatesUniqueNames()
    {
        var path1 = await _manager.CreateTempSpecFileAsync("content1", CancellationToken.None);
        var path2 = await _manager.CreateTempSpecFileAsync("content2", CancellationToken.None);

        try
        {
            await Assert.That(path1).IsNotEqualTo(path2);
        }
        finally
        {
            _manager.Cleanup(path1, path2);
        }
    }

    [Test]
    public async Task CreateTempSpecFileAsync_FileHasCsExtension()
    {
        var path = await _manager.CreateTempSpecFileAsync("content", CancellationToken.None);

        try
        {
            await Assert.That(path).EndsWith(".cs");
        }
        finally
        {
            _manager.Cleanup(path);
        }
    }

    [Test]
    public async Task CreateTempSpecFileAsync_FileInTempDirectory()
    {
        var path = await _manager.CreateTempSpecFileAsync("content", CancellationToken.None);

        try
        {
            var directory = Path.GetDirectoryName(path);
            await Assert.That(directory).IsEqualTo(_manager.TempDirectory);
        }
        finally
        {
            _manager.Cleanup(path);
        }
    }

    #endregion

    #region CreateTempJsonOutputPath

    [Test]
    public async Task CreateTempJsonOutputPath_ReturnsPath()
    {
        var path = _manager.CreateTempJsonOutputPath();

        await Assert.That(path).IsNotNull();
        await Assert.That(path).IsNotEmpty();
    }

    [Test]
    public async Task CreateTempJsonOutputPath_HasJsonExtension()
    {
        var path = _manager.CreateTempJsonOutputPath();

        await Assert.That(path).EndsWith(".json");
    }

    [Test]
    public async Task CreateTempJsonOutputPath_PathInTempDirectory()
    {
        var path = _manager.CreateTempJsonOutputPath();

        var directory = Path.GetDirectoryName(path);
        await Assert.That(directory).IsEqualTo(_manager.TempDirectory);
    }

    [Test]
    public async Task CreateTempJsonOutputPath_DoesNotCreateFile()
    {
        var path = _manager.CreateTempJsonOutputPath();

        await Assert.That(File.Exists(path)).IsFalse();
    }

    [Test]
    public async Task CreateTempJsonOutputPath_CreatesUniquePaths()
    {
        var path1 = _manager.CreateTempJsonOutputPath();
        var path2 = _manager.CreateTempJsonOutputPath();

        await Assert.That(path1).IsNotEqualTo(path2);
    }

    #endregion

    #region Cleanup

    [Test]
    public async Task Cleanup_DeletesExistingFile()
    {
        var path = await _manager.CreateTempSpecFileAsync("content", CancellationToken.None);
        await Assert.That(File.Exists(path)).IsTrue();

        _manager.Cleanup(path);

        await Assert.That(File.Exists(path)).IsFalse();
    }

    [Test]
    public async Task Cleanup_HandlesNullPathElement()
    {
        // An array containing null elements should be handled gracefully
        _manager.Cleanup(new string?[] { null });

        await Assert.That(true).IsTrue(); // If we get here, no exception was thrown
    }

    [Test]
    public async Task Cleanup_HandlesEmptyPath()
    {
        // Should not throw
        _manager.Cleanup("");

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Cleanup_HandlesNonexistentFile()
    {
        var path = Path.Combine(_manager.TempDirectory, "nonexistent-file.cs");

        // Should not throw
        _manager.Cleanup(path);

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Cleanup_HandlesMultiplePaths()
    {
        var path1 = await _manager.CreateTempSpecFileAsync("content1", CancellationToken.None);
        var path2 = await _manager.CreateTempSpecFileAsync("content2", CancellationToken.None);

        _manager.Cleanup(path1, path2);

        await Assert.That(File.Exists(path1)).IsFalse();
        await Assert.That(File.Exists(path2)).IsFalse();
    }

    [Test]
    public async Task Cleanup_HandlesMixedValidAndInvalidPaths()
    {
        var validPath = await _manager.CreateTempSpecFileAsync("content", CancellationToken.None);
        var invalidPath = Path.Combine(_manager.TempDirectory, "nonexistent.cs");

        // Should not throw even with mixed paths including null elements
        _manager.Cleanup(new string?[] { validPath, null, invalidPath, "" });

        await Assert.That(File.Exists(validPath)).IsFalse();
    }

    #endregion
}

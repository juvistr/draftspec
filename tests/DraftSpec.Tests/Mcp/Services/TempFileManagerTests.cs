using DraftSpec.Mcp.Services;
using DraftSpec.Tests.Infrastructure.Mocks;
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

    #region Local Packages / NuGet Config

    [Test]
    [Category("Unix")]
    public async Task Constructor_WithLocalPackages_CreatesNuGetConfig()
    {
        if (OperatingSystem.IsWindows()) return;

        var localPackagesDir = "/tmp/draftspec-packages";
        var dummyPackage = Path.Combine(localPackagesDir, "test.nupkg");

        try
        {
            // Create local packages directory with a dummy nupkg file
            Directory.CreateDirectory(localPackagesDir);
            await File.WriteAllTextAsync(dummyPackage, "dummy package content");

            // Create a new manager - this should trigger EnsureNuGetConfig
            var logger = NullLogger<TempFileManager>.Instance;
            var manager = new TempFileManager(logger);

            // Verify NuGet.config was created
            var nugetConfigPath = Path.Combine(manager.TempDirectory, "NuGet.config");
            await Assert.That(File.Exists(nugetConfigPath)).IsTrue();

            // Verify it contains the local packages path
            var content = await File.ReadAllTextAsync(nugetConfigPath);
            await Assert.That(content).Contains(localPackagesDir);
            await Assert.That(content).Contains("nuget.org");

            // Cleanup NuGet.config
            if (File.Exists(nugetConfigPath))
                File.Delete(nugetConfigPath);
        }
        finally
        {
            // Cleanup local packages directory
            if (File.Exists(dummyPackage))
                File.Delete(dummyPackage);
            if (Directory.Exists(localPackagesDir))
                Directory.Delete(localPackagesDir);
        }
    }

    [Test]
    [Category("Unix")]
    public async Task Constructor_WithLocalPackages_SetsRestrictivePermissionsOnNuGetConfig()
    {
        if (OperatingSystem.IsWindows()) return;

        var localPackagesDir = "/tmp/draftspec-packages";
        var dummyPackage = Path.Combine(localPackagesDir, "test.nupkg");

        try
        {
            // Create local packages directory with a dummy nupkg file
            Directory.CreateDirectory(localPackagesDir);
            await File.WriteAllTextAsync(dummyPackage, "dummy package content");

            // Create a new manager - this should trigger EnsureNuGetConfig
            var logger = NullLogger<TempFileManager>.Instance;
            var manager = new TempFileManager(logger);

            // Verify NuGet.config has restrictive permissions
            var nugetConfigPath = Path.Combine(manager.TempDirectory, "NuGet.config");
            var mode = File.GetUnixFileMode(nugetConfigPath);
            await Assert.That(mode).IsEqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite);

            // Cleanup NuGet.config
            if (File.Exists(nugetConfigPath))
                File.Delete(nugetConfigPath);
        }
        finally
        {
            // Cleanup local packages directory
            if (File.Exists(dummyPackage))
                File.Delete(dummyPackage);
            if (Directory.Exists(localPackagesDir))
                Directory.Delete(localPackagesDir);
        }
    }

    [Test]
    [Category("Unix")]
    public async Task Constructor_WithExistingNuGetConfig_DoesNotOverwrite()
    {
        if (OperatingSystem.IsWindows()) return;

        var localPackagesDir = "/tmp/draftspec-packages";
        var dummyPackage = Path.Combine(localPackagesDir, "test.nupkg");
        var tempDir = Path.Combine(Path.GetTempPath(), "draftspec-mcp");

        try
        {
            // Create local packages directory with a dummy nupkg file
            Directory.CreateDirectory(localPackagesDir);
            await File.WriteAllTextAsync(dummyPackage, "dummy package content");

            // Pre-create a NuGet.config with custom content
            Directory.CreateDirectory(tempDir);
            var nugetConfigPath = Path.Combine(tempDir, "NuGet.config");
            var originalContent = "<!-- existing config -->";
            await File.WriteAllTextAsync(nugetConfigPath, originalContent);

            // Create a new manager - should NOT overwrite existing config
            var logger = NullLogger<TempFileManager>.Instance;
            _ = new TempFileManager(logger);

            // Verify content was not changed
            var content = await File.ReadAllTextAsync(nugetConfigPath);
            await Assert.That(content).IsEqualTo(originalContent);

            // Cleanup NuGet.config
            if (File.Exists(nugetConfigPath))
                File.Delete(nugetConfigPath);
        }
        finally
        {
            // Cleanup local packages directory
            if (File.Exists(dummyPackage))
                File.Delete(dummyPackage);
            if (Directory.Exists(localPackagesDir))
                Directory.Delete(localPackagesDir);
        }
    }

    #endregion

    #region Cleanup Exception Handling

    [Test]
    [Category("Unix")]
    public async Task Cleanup_WhenFileCannotBeDeleted_LogsWarningAndContinues()
    {
        if (OperatingSystem.IsWindows()) return;

        // Create a file in a directory, then make the directory read-only
        var testDir = Path.Combine(_manager.TempDirectory, $"readonly-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        var filePath = Path.Combine(testDir, "locked-file.txt");
        await File.WriteAllTextAsync(filePath, "content");

        try
        {
            // Make directory read-only (prevents file deletion on Unix)
            File.SetUnixFileMode(testDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);

            // This should NOT throw - it should log a warning and continue
            _manager.Cleanup(filePath);

            // File should still exist because deletion failed
            await Assert.That(File.Exists(filePath)).IsTrue();
        }
        finally
        {
            // Restore permissions for cleanup
            File.SetUnixFileMode(testDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            if (File.Exists(filePath))
                File.Delete(filePath);
            if (Directory.Exists(testDir))
                Directory.Delete(testDir);
        }
    }

    #endregion

    #region Unix File Permissions

    [Test]
    [Category("Unix")]
    public async Task CreateTempSpecFileAsync_OnUnix_SetsRestrictivePermissions()
    {
        if (OperatingSystem.IsWindows()) return;

        var path = await _manager.CreateTempSpecFileAsync("content", CancellationToken.None);

        try
        {
            var mode = File.GetUnixFileMode(path);

            // Should be 0600 (UserRead | UserWrite only)
            await Assert.That(mode).IsEqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        finally
        {
            _manager.Cleanup(path);
        }
    }

    [Test]
    [Category("Unix")]
    public async Task CreateTempSpecFileAsync_OnUnix_NoGroupOrOtherPermissions()
    {
        if (OperatingSystem.IsWindows()) return;

        var path = await _manager.CreateTempSpecFileAsync("content", CancellationToken.None);

        try
        {
            var mode = File.GetUnixFileMode(path);

            // Verify no group permissions
            await Assert.That(mode.HasFlag(UnixFileMode.GroupRead)).IsFalse();
            await Assert.That(mode.HasFlag(UnixFileMode.GroupWrite)).IsFalse();
            await Assert.That(mode.HasFlag(UnixFileMode.GroupExecute)).IsFalse();

            // Verify no other permissions
            await Assert.That(mode.HasFlag(UnixFileMode.OtherRead)).IsFalse();
            await Assert.That(mode.HasFlag(UnixFileMode.OtherWrite)).IsFalse();
            await Assert.That(mode.HasFlag(UnixFileMode.OtherExecute)).IsFalse();
        }
        finally
        {
            _manager.Cleanup(path);
        }
    }

    [Test]
    [Category("Unix")]
    public async Task CreateTempSpecFileAsync_OnUnix_OwnerHasReadWriteAccess()
    {
        if (OperatingSystem.IsWindows()) return;

        var path = await _manager.CreateTempSpecFileAsync("content", CancellationToken.None);

        try
        {
            var mode = File.GetUnixFileMode(path);

            // Verify owner has read and write permissions
            await Assert.That(mode.HasFlag(UnixFileMode.UserRead)).IsTrue();
            await Assert.That(mode.HasFlag(UnixFileMode.UserWrite)).IsTrue();

            // Verify owner does not have execute permission
            await Assert.That(mode.HasFlag(UnixFileMode.UserExecute)).IsFalse();
        }
        finally
        {
            _manager.Cleanup(path);
        }
    }

    #endregion

    #region Mock Permission Setter Tests

    [Test]
    public async Task CreateTempSpecFileAsync_CallsPermissionSetterWithCorrectMode()
    {
        var mockSetter = new MockUnixPermissionSetter();
        var logger = NullLogger<TempFileManager>.Instance;
        var manager = new TempFileManager(logger, mockSetter);

        var path = await manager.CreateTempSpecFileAsync("content", CancellationToken.None);

        try
        {
            await Assert.That(mockSetter.SetModeCalls).Count().IsEqualTo(1);
            await Assert.That(mockSetter.SetModeCalls[0].Path).IsEqualTo(path);
            await Assert.That(mockSetter.SetModeCalls[0].Mode).IsEqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        finally
        {
            manager.Cleanup(path);
        }
    }

    [Test]
    public async Task CreateTempSpecFileAsync_WhenPermissionsFail_StillReturnsFilePath()
    {
        var mockSetter = new MockUnixPermissionSetter
        {
            ThrowOnSetMode = new IOException("Permission denied")
        };
        var logger = NullLogger<TempFileManager>.Instance;
        var manager = new TempFileManager(logger, mockSetter);

        var path = await manager.CreateTempSpecFileAsync("content", CancellationToken.None);

        try
        {
            // Should still return a valid path even though permissions failed
            await Assert.That(path).IsNotNull();
            await Assert.That(File.Exists(path)).IsTrue();

            // Verify the content was still written
            var content = await File.ReadAllTextAsync(path);
            await Assert.That(content).IsEqualTo("content");
        }
        finally
        {
            manager.Cleanup(path);
        }
    }

    [Test]
    public async Task CreateTempSpecFileAsync_WhenPermissionsFail_DoesNotThrow()
    {
        var mockSetter = new MockUnixPermissionSetter
        {
            ThrowOnSetMode = new UnauthorizedAccessException("Access denied")
        };
        var logger = NullLogger<TempFileManager>.Instance;
        var manager = new TempFileManager(logger, mockSetter);

        // This should not throw even though permission setting fails
        var path = await manager.CreateTempSpecFileAsync("content", CancellationToken.None);

        manager.Cleanup(path);
        await Assert.That(true).IsTrue(); // If we get here, no exception was thrown
    }

    [Test]
    public async Task CreateTempSpecFileAsync_MultipleFiles_CallsPermissionSetterForEach()
    {
        var mockSetter = new MockUnixPermissionSetter();
        var logger = NullLogger<TempFileManager>.Instance;
        var manager = new TempFileManager(logger, mockSetter);

        var path1 = await manager.CreateTempSpecFileAsync("content1", CancellationToken.None);
        var path2 = await manager.CreateTempSpecFileAsync("content2", CancellationToken.None);

        try
        {
            await Assert.That(mockSetter.SetModeCalls).Count().IsEqualTo(2);
            await Assert.That(mockSetter.SetModeCalls[0].Path).IsEqualTo(path1);
            await Assert.That(mockSetter.SetModeCalls[1].Path).IsEqualTo(path2);
        }
        finally
        {
            manager.Cleanup(path1, path2);
        }
    }

    #endregion
}

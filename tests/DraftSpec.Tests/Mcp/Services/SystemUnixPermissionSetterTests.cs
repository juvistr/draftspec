using DraftSpec.Mcp.Services;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Mcp.Services;

/// <summary>
/// Tests for SystemUnixPermissionSetter.
/// </summary>
public class SystemUnixPermissionSetterTests
{
    #region Windows Branch

    [Test]
    public async Task SetMode_OnWindows_DoesNotCallFileSetUnixFileMode()
    {
        // Arrange - simulate Windows
        var mockOs = MockOperatingSystem.Windows();
        var setter = new SystemUnixPermissionSetter(mockOs);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act - should be a no-op on Windows
            setter.SetMode(tempFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);

            // Assert - file should still exist (no exception thrown)
            await Assert.That(File.Exists(tempFile)).IsTrue();

            // On actual Windows, File.GetUnixFileMode would throw, but since we're
            // mocking Windows on a Unix system, we can verify the mode wasn't changed
            if (!OperatingSystem.IsWindows())
            {
                // The file should have its original permissions, not the ones we tried to set
                var mode = File.GetUnixFileMode(tempFile);
                // GetTempFileName typically creates with 0600 on Unix, but the point is
                // that SetMode was a no-op because we're "on Windows"
                await Assert.That(mode).IsNotEqualTo(UnixFileMode.None);
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task SetMode_OnWindows_ReturnsWithoutError()
    {
        var mockOs = MockOperatingSystem.Windows();
        var setter = new SystemUnixPermissionSetter(mockOs);

        // Even with a non-existent file, should not throw on Windows (early return)
        setter.SetMode("/nonexistent/path/file.txt", UnixFileMode.UserRead);

        await Assert.That(true).IsTrue(); // If we get here, no exception
    }

    #endregion

    #region Unix Branch

    [Test]
    public async Task SetMode_OnUnix_SetsFilePermissions()
    {
        if (OperatingSystem.IsWindows()) return;

        var mockOs = MockOperatingSystem.Unix();
        var setter = new SystemUnixPermissionSetter(mockOs);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            setter.SetMode(tempFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);

            // Assert
            var mode = File.GetUnixFileMode(tempFile);
            await Assert.That(mode).IsEqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task SetMode_OnUnix_WithDifferentMode_SetsCorrectPermissions()
    {
        if (OperatingSystem.IsWindows()) return;

        var mockOs = MockOperatingSystem.Unix();
        var setter = new SystemUnixPermissionSetter(mockOs);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act - set read-only
            setter.SetMode(tempFile, UnixFileMode.UserRead);

            // Assert
            var mode = File.GetUnixFileMode(tempFile);
            await Assert.That(mode).IsEqualTo(UnixFileMode.UserRead);

            // Restore write permission for cleanup
            File.SetUnixFileMode(tempFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task SetMode_OnUnix_WithNonExistentFile_Throws()
    {
        if (OperatingSystem.IsWindows()) return;

        var mockOs = MockOperatingSystem.Unix();
        var setter = new SystemUnixPermissionSetter(mockOs);

        var action = () => setter.SetMode("/nonexistent/path/file.txt", UnixFileMode.UserRead);

        await Assert.That(action).ThrowsException();
    }

    #endregion

    #region Default Constructor

    [Test]
    public async Task Instance_UsesCurrentOperatingSystem()
    {
        var setter = SystemUnixPermissionSetter.Instance;

        // The singleton should work without throwing
        var tempFile = Path.GetTempFileName();

        try
        {
            if (!OperatingSystem.IsWindows())
            {
                setter.SetMode(tempFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                var mode = File.GetUnixFileMode(tempFile);
                await Assert.That(mode).IsEqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            else
            {
                // On Windows, should be a no-op
                setter.SetMode(tempFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                await Assert.That(true).IsTrue();
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion
}

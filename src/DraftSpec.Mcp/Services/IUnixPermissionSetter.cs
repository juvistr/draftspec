namespace DraftSpec.Mcp.Services;

/// <summary>
/// Abstraction for setting Unix file permissions.
/// Enables testing of permission-setting code paths.
/// </summary>
public interface IUnixPermissionSetter
{
    /// <summary>
    /// Sets Unix file mode on the specified path.
    /// No-op on Windows for production implementations.
    /// </summary>
    /// <param name="path">The file path to set permissions on.</param>
    /// <param name="mode">The Unix file mode to set.</param>
    void SetMode(string path, UnixFileMode mode);
}

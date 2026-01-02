namespace DraftSpec.Mcp.Services;

/// <summary>
/// Production implementation that delegates to File.SetUnixFileMode.
/// </summary>
public class SystemUnixPermissionSetter : IUnixPermissionSetter
{
    private readonly IOperatingSystem _os;

    /// <summary>
    /// Singleton instance for convenience (uses real OS detection).
    /// </summary>
    public static SystemUnixPermissionSetter Instance { get; } = new();

    /// <summary>
    /// Creates a new instance with the specified operating system provider.
    /// </summary>
    public SystemUnixPermissionSetter(IOperatingSystem? os = null)
    {
        _os = os ?? CurrentOperatingSystem.Instance;
    }

    /// <inheritdoc />
    public void SetMode(string path, UnixFileMode mode)
    {
        if (_os.IsWindows)
            return;

        // CA1416: We're guarding with a runtime OS check via IOperatingSystem
#pragma warning disable CA1416
        File.SetUnixFileMode(path, mode);
#pragma warning restore CA1416
    }
}

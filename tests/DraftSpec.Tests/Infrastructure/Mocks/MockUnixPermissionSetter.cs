using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IUnixPermissionSetter for testing.
/// </summary>
public class MockUnixPermissionSetter : IUnixPermissionSetter
{
    /// <summary>
    /// Records all calls to SetMode.
    /// </summary>
    public List<(string Path, UnixFileMode Mode)> SetModeCalls { get; } = [];

    /// <summary>
    /// When set, SetMode will throw this exception.
    /// </summary>
    public Exception? ThrowOnSetMode { get; set; }

    /// <inheritdoc />
    public void SetMode(string path, UnixFileMode mode)
    {
        if (ThrowOnSetMode != null)
            throw ThrowOnSetMode;

        SetModeCalls.Add((path, mode));
    }

    /// <summary>
    /// Resets the mock state.
    /// </summary>
    public void Reset()
    {
        SetModeCalls.Clear();
        ThrowOnSetMode = null;
    }
}

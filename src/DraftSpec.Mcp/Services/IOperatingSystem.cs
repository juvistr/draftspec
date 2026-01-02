namespace DraftSpec.Mcp.Services;

/// <summary>
/// Abstraction for operating system detection.
/// Enables testing of platform-specific code paths.
/// </summary>
public interface IOperatingSystem
{
    /// <summary>
    /// Gets whether the current operating system is Windows.
    /// </summary>
    bool IsWindows { get; }
}

namespace DraftSpec.Abstractions;

/// <summary>
/// Abstraction for operating system detection, enabling OS-agnostic testing.
/// </summary>
public interface IOperatingSystem
{
    /// <summary>
    /// Returns true if the current operating system is Windows.
    /// </summary>
    bool IsWindows { get; }

    /// <summary>
    /// Returns true if the current operating system is macOS.
    /// </summary>
    bool IsMacOS { get; }
}

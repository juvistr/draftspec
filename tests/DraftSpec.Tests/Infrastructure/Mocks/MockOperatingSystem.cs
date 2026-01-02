using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IOperatingSystem for testing platform-specific code.
/// </summary>
public class MockOperatingSystem : IOperatingSystem
{
    /// <summary>
    /// Gets or sets whether IsWindows returns true.
    /// </summary>
    public bool IsWindows { get; set; }

    /// <summary>
    /// Creates a mock that simulates Windows.
    /// </summary>
    public static MockOperatingSystem Windows() => new() { IsWindows = true };

    /// <summary>
    /// Creates a mock that simulates Unix/Linux/macOS.
    /// </summary>
    public static MockOperatingSystem Unix() => new() { IsWindows = false };
}

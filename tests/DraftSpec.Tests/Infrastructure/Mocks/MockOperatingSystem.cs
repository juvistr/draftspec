namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IOperatingSystem for testing platform-specific code.
/// </summary>
public class MockOperatingSystem : IOperatingSystem
{
    /// <inheritdoc />
    public bool IsWindows { get; set; }

    /// <inheritdoc />
    public bool IsMacOS { get; set; }

    /// <summary>
    /// Creates a mock that simulates Windows.
    /// </summary>
    public static MockOperatingSystem Windows() => new() { IsWindows = true };

    /// <summary>
    /// Creates a mock that simulates Unix/Linux.
    /// </summary>
    public static MockOperatingSystem Unix() => new() { IsWindows = false };

    /// <summary>
    /// Creates a mock that simulates macOS.
    /// </summary>
    public static MockOperatingSystem MacOS() => new() { IsMacOS = true };

    /// <summary>
    /// Configures this mock to simulate Windows.
    /// </summary>
    public MockOperatingSystem WithWindows()
    {
        IsWindows = true;
        IsMacOS = false;
        return this;
    }

    /// <summary>
    /// Configures this mock to simulate macOS.
    /// </summary>
    public MockOperatingSystem WithMacOS()
    {
        IsWindows = false;
        IsMacOS = true;
        return this;
    }

    /// <summary>
    /// Configures this mock to simulate Linux.
    /// </summary>
    public MockOperatingSystem WithLinux()
    {
        IsWindows = false;
        IsMacOS = false;
        return this;
    }
}

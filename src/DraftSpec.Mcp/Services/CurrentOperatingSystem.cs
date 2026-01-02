namespace DraftSpec.Mcp.Services;

/// <summary>
/// Production implementation that delegates to System.OperatingSystem.
/// </summary>
public class CurrentOperatingSystem : IOperatingSystem
{
    /// <summary>
    /// Singleton instance for convenience.
    /// </summary>
    public static CurrentOperatingSystem Instance { get; } = new();

    /// <inheritdoc />
    public bool IsWindows => OperatingSystem.IsWindows();
}

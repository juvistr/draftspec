namespace DraftSpec.Abstractions;

/// <summary>
/// Production implementation that delegates to System.OperatingSystem.
/// </summary>
public sealed class SystemOperatingSystem : IOperatingSystem
{
    /// <inheritdoc />
    public bool IsWindows => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public bool IsMacOS => OperatingSystem.IsMacOS();
}

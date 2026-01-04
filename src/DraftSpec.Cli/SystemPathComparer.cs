namespace DraftSpec.Cli;

/// <summary>
/// Production implementation that provides OS-appropriate path comparison.
/// Uses case-insensitive comparison on Windows, case-sensitive elsewhere.
/// </summary>
public sealed class SystemPathComparer : IPathComparer
{
    /// <summary>
    /// Creates a new SystemPathComparer using the specified operating system.
    /// </summary>
    public SystemPathComparer(IOperatingSystem operatingSystem)
    {
        Comparison = operatingSystem.IsWindows
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        Comparer = operatingSystem.IsWindows
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    /// <inheritdoc />
    public StringComparison Comparison { get; }

    /// <inheritdoc />
    public StringComparer Comparer { get; }
}

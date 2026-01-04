namespace DraftSpec.Abstractions;

/// <summary>
/// Provides OS-appropriate string comparison for file paths.
/// Uses case-insensitive comparison on Windows, case-sensitive elsewhere.
/// </summary>
public interface IPathComparer
{
    /// <summary>
    /// Gets the appropriate StringComparison for file paths.
    /// OrdinalIgnoreCase on Windows, Ordinal elsewhere.
    /// </summary>
    StringComparison Comparison { get; }

    /// <summary>
    /// Gets the appropriate StringComparer for file paths.
    /// OrdinalIgnoreCase on Windows, Ordinal elsewhere.
    /// </summary>
    StringComparer Comparer { get; }
}

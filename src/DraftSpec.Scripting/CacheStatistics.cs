namespace DraftSpec.Scripting;

/// <summary>
/// Statistics about the script compilation cache.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Number of cached entries.
    /// </summary>
    public int EntryCount { get; init; }

    /// <summary>
    /// Total size of cached assemblies in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Path to the cache directory.
    /// </summary>
    public string CacheDirectory { get; init; } = "";
}

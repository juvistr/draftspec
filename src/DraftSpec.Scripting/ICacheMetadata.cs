namespace DraftSpec.Scripting;

/// <summary>
/// Common metadata stored alongside cached entries.
/// </summary>
public interface ICacheMetadata
{
    /// <summary>
    /// Unique cache key computed from source files and content.
    /// </summary>
    string CacheKey { get; set; }

    /// <summary>
    /// Absolute path to the main source file.
    /// </summary>
    string SourcePath { get; set; }

    /// <summary>
    /// All source files including dependencies (e.g., #load files).
    /// </summary>
    List<string> SourceFiles { get; set; }

    /// <summary>
    /// SHA256 hashes for each source file.
    /// </summary>
    Dictionary<string, string> SourceFileHashes { get; set; }

    /// <summary>
    /// DraftSpec version used when caching.
    /// </summary>
    string DraftSpecVersion { get; set; }

    /// <summary>
    /// UTC timestamp when the entry was cached.
    /// </summary>
    DateTime CachedAtUtc { get; set; }
}

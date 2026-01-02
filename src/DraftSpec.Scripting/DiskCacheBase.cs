using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DraftSpec.Scripting;

/// <summary>
/// Base class for disk-based caching implementations.
/// Provides common functionality for hash computation, cache validation, and file operations.
/// </summary>
public abstract class DiskCacheBase
{
    private const string MetadataExtension = ".meta.json";

    /// <summary>
    /// Directory where cache files are stored.
    /// </summary>
    protected string CacheDirectory { get; }

    /// <summary>
    /// Version string used for cache invalidation when DraftSpec is updated.
    /// </summary>
    protected string Version { get; }

    /// <summary>
    /// Logger for cache operations.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Creates a new disk cache base.
    /// </summary>
    /// <param name="cacheDirectory">Directory where cache files will be stored.</param>
    /// <param name="version">Version string for cache invalidation.</param>
    /// <param name="logger">Optional logger for cache operations.</param>
    protected DiskCacheBase(string cacheDirectory, string version, ILogger? logger = null)
    {
        CacheDirectory = cacheDirectory;
        Version = version;
        Logger = logger ?? NullLogger.Instance;
    }

    #region Static Hashing Methods

    /// <summary>
    /// Computes SHA256 hash of a string.
    /// </summary>
    protected static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Computes SHA256 hash of a file's contents.
    /// </summary>
    protected static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Computes SHA256 hashes for all source files.
    /// </summary>
    protected static Dictionary<string, string> ComputeFileHashes(IReadOnlyList<string> sourceFiles)
    {
        return sourceFiles
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(f => f, ComputeFileHash, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Cache Key Computation

    /// <summary>
    /// Computes a cache key based on source path and dependencies.
    /// </summary>
    protected string ComputeCacheKey(string sourcePath, IReadOnlyList<string> sourceFiles)
    {
        var fileHashes = ComputeFileHashes(sourceFiles);
        return ComputeCacheKeyFromHashes(sourcePath, fileHashes, additionalContent: null);
    }

    /// <summary>
    /// Computes a cache key based on source path, dependencies, and additional content.
    /// </summary>
    protected string ComputeCacheKey(string sourcePath, IReadOnlyList<string> sourceFiles, string? additionalContent)
    {
        var fileHashes = ComputeFileHashes(sourceFiles);
        return ComputeCacheKeyFromHashes(sourcePath, fileHashes, additionalContent);
    }

    /// <summary>
    /// Computes a cache key using pre-computed file hashes.
    /// </summary>
    protected string ComputeCacheKeyFromHashes(
        string sourcePath,
        Dictionary<string, string> fileHashes,
        string? additionalContent)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.AppendLine(Version);
        keyBuilder.AppendLine(sourcePath);

        // Include all dependency file hashes in sorted order for determinism
        foreach (var (file, hash) in fileHashes.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            keyBuilder.AppendLine($"{file}:{hash}");
        }

        if (additionalContent != null)
        {
            keyBuilder.AppendLine(ComputeHash(additionalContent));
        }

        return ComputeHash(keyBuilder.ToString())[..16]; // Use first 16 chars of hash
    }

    #endregion

    #region Cache Validation

    /// <summary>
    /// Validates that a cache entry is still valid using pre-computed hashes.
    /// </summary>
    protected bool ValidateHashesMatch(ICacheMetadata metadata, Dictionary<string, string> currentFileHashes)
    {
        // Check DraftSpec version
        if (metadata.DraftSpecVersion != Version)
            return false;

        // Check that all source files match
        if (metadata.SourceFiles.Count != currentFileHashes.Count)
            return false;

        foreach (var (file, currentHash) in currentFileHashes)
        {
            if (!metadata.SourceFileHashes.TryGetValue(file, out var cachedHash))
                return false;

            if (currentHash != cachedHash)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a cache entry is still valid by computing file hashes.
    /// </summary>
    protected bool ValidateFilesUnchanged(ICacheMetadata metadata, IReadOnlyList<string> currentSourceFiles)
    {
        // Check DraftSpec version
        if (metadata.DraftSpecVersion != Version)
            return false;

        // Check that all source files still exist and haven't changed
        if (metadata.SourceFiles.Count != currentSourceFiles.Count)
            return false;

        foreach (var file in currentSourceFiles)
        {
            if (!File.Exists(file))
                return false;

            if (!metadata.SourceFileHashes.TryGetValue(file, out var cachedHash))
                return false;

            var currentHash = ComputeFileHash(file);
            if (currentHash != cachedHash)
                return false;
        }

        return true;
    }

    #endregion

    #region File Operations

    /// <summary>
    /// Ensures the cache directory exists.
    /// </summary>
    protected void EnsureCacheDirectoryExists()
    {
        Directory.CreateDirectory(CacheDirectory);
    }

    /// <summary>
    /// Gets the full path for a metadata file.
    /// </summary>
    protected string GetMetadataPath(string cacheKey) =>
        Path.Combine(CacheDirectory, cacheKey + MetadataExtension);

    /// <summary>
    /// Gets the full path for a file with the given cache key and extension.
    /// </summary>
    protected string GetFilePath(string cacheKey, string extension) =>
        Path.Combine(CacheDirectory, cacheKey + extension);

    /// <summary>
    /// Deletes all files associated with a cache entry.
    /// </summary>
    protected void DeleteFiles(string cacheKey, params string[] extensions)
    {
        try
        {
            var metaPath = GetMetadataPath(cacheKey);
            if (File.Exists(metaPath)) File.Delete(metaPath);

            foreach (var ext in extensions)
            {
                var path = GetFilePath(cacheKey, ext);
                if (File.Exists(path)) File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to delete cache entry {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Loads JSON data from a file synchronously.
    /// </summary>
    protected T? LoadJson<T>(string path, JsonSerializerOptions options)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to load JSON from {Path}", path);
            return default;
        }
    }

    /// <summary>
    /// Loads JSON data from a file asynchronously.
    /// </summary>
    protected async Task<T?> LoadJsonAsync<T>(string path, JsonSerializerOptions options, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, options, ct);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to load JSON from {Path}", path);
            return default;
        }
    }

    /// <summary>
    /// Saves JSON data to a file atomically using temp file + rename.
    /// </summary>
    protected static void AtomicWriteJson<T>(string path, T data, JsonSerializerOptions options)
    {
        var tempPath = path + ".tmp";
        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>
    /// Saves JSON data to a file atomically using temp file + rename (async).
    /// </summary>
    protected static async Task AtomicWriteJsonAsync<T>(
        string path,
        T data,
        JsonSerializerOptions options,
        CancellationToken ct)
    {
        var tempPath = path + ".tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await JsonSerializer.SerializeAsync(stream, data, options, ct);
        }
        File.Move(tempPath, path, overwrite: true);
    }

    #endregion

    #region Public Operations

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public virtual void Clear()
    {
        try
        {
            if (Directory.Exists(CacheDirectory))
            {
                Directory.Delete(CacheDirectory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to clear cache directory {CacheDirectory}", CacheDirectory);
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <param name="dataExtension">The extension of data files to count (e.g., ".dll" or ".result.json").</param>
    protected CacheStatistics GetStatistics(string dataExtension)
    {
        try
        {
            if (!Directory.Exists(CacheDirectory))
                return new CacheStatistics();

            var metaFiles = Directory.GetFiles(CacheDirectory, "*" + MetadataExtension);
            var dataFiles = Directory.GetFiles(CacheDirectory, "*" + dataExtension);
            var totalSize = dataFiles.Sum(f => new FileInfo(f).Length);

            return new CacheStatistics
            {
                EntryCount = metaFiles.Length,
                TotalSizeBytes = totalSize,
                CacheDirectory = CacheDirectory
            };
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to read cache statistics from {CacheDirectory}", CacheDirectory);
            return new CacheStatistics();
        }
    }

    #endregion
}

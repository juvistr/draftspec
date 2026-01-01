using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DraftSpec.Scripting;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Disk-based cache for static parse results.
/// Stores parsed spec structures to avoid re-parsing unchanged files.
/// </summary>
public sealed class StaticParseResultCache
{
    private const string CacheDirectoryName = ".draftspec";
    private const string ParsingCacheSubdirectory = "cache/parsing";
    private const string MetadataExtension = ".meta.json";
    private const string ResultExtension = ".result.json";

    private readonly string _cacheDirectory;
    private readonly string _draftSpecVersion;

    /// <summary>
    /// JSON serialization options for cache data.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Creates a new static parse result cache.
    /// </summary>
    /// <param name="projectDirectory">The project directory containing .draftspec folder.</param>
    public StaticParseResultCache(string projectDirectory)
    {
        _cacheDirectory = Path.Combine(projectDirectory, CacheDirectoryName, ParsingCacheSubdirectory);
        _draftSpecVersion = typeof(StaticParseResult).Assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    /// <summary>
    /// Tries to get a cached parse result.
    /// </summary>
    /// <param name="sourcePath">Absolute path to the main source file.</param>
    /// <param name="sourceFiles">All source files including #load dependencies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True and the result if cache hit, false otherwise.</returns>
    public async Task<(bool success, StaticParseResult? result)> TryGetCachedAsync(
        string sourcePath,
        IReadOnlyList<string> sourceFiles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Compute file hashes once and reuse for both cache key and validation
            var fileHashes = ComputeFileHashes(sourceFiles);

            var cacheKey = ComputeCacheKeyFromHashes(sourcePath, fileHashes);
            var metadataPath = GetMetadataPath(cacheKey);

            if (!File.Exists(metadataPath))
                return (false, null);

            var metadata = await LoadMetadataAsync(metadataPath, cancellationToken);
            if (metadata == null)
                return (false, null);

            // Validate cache using pre-computed hashes
            if (!IsCacheValidWithHashes(metadata, fileHashes))
            {
                // Remove stale cache files
                DeleteCacheEntry(cacheKey);
                return (false, null);
            }

            var resultPath = GetResultPath(cacheKey);
            if (!File.Exists(resultPath))
                return (false, null);

            // Load and return the cached result
            var result = await LoadResultAsync(resultPath, cancellationToken);
            if (result == null)
                return (false, null);

            return (true, result);
        }
        catch
        {
            // Cache read errors should fall back to parsing
            return (false, null);
        }
    }

    /// <summary>
    /// Caches a parse result for future use.
    /// </summary>
    /// <param name="sourcePath">Absolute path to the main source file.</param>
    /// <param name="sourceFiles">All source files including #load dependencies.</param>
    /// <param name="result">The parse result to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CacheAsync(
        string sourcePath,
        IReadOnlyList<string> sourceFiles,
        StaticParseResult result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureCacheDirectoryExists();

            var cacheKey = ComputeCacheKey(sourcePath, sourceFiles);

            // Save metadata
            var metadata = new CacheMetadata
            {
                CacheKey = cacheKey,
                SourcePath = sourcePath,
                SourceFiles = sourceFiles.ToList(),
                SourceFileHashes = sourceFiles.ToDictionary(f => f, ComputeFileHash),
                DraftSpecVersion = _draftSpecVersion,
                CachedAtUtc = DateTime.UtcNow
            };

            await SaveMetadataAsync(GetMetadataPath(cacheKey), metadata, cancellationToken);

            // Save result
            var resultData = new CachedResult
            {
                Specs = result.Specs.Select(s => new CachedSpec
                {
                    Description = s.Description,
                    ContextPath = s.ContextPath.ToList(),
                    LineNumber = s.LineNumber,
                    Type = s.Type,
                    IsPending = s.IsPending
                }).ToList(),
                Warnings = result.Warnings.ToList(),
                IsComplete = result.IsComplete
            };

            await SaveResultAsync(GetResultPath(cacheKey), resultData, cancellationToken);
        }
        catch
        {
            // Cache write errors should not fail parsing
        }
    }

    /// <summary>
    /// Clears all cached parse results.
    /// </summary>
    public void Clear()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore errors when clearing cache
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
                return new CacheStatistics();

            var metaFiles = Directory.GetFiles(_cacheDirectory, "*" + MetadataExtension);
            var resultFiles = Directory.GetFiles(_cacheDirectory, "*" + ResultExtension);
            var totalSize = resultFiles.Sum(f => new FileInfo(f).Length);

            return new CacheStatistics
            {
                EntryCount = metaFiles.Length,
                TotalSizeBytes = totalSize,
                CacheDirectory = _cacheDirectory
            };
        }
        catch
        {
            return new CacheStatistics();
        }
    }

    /// <summary>
    /// Computes SHA256 hashes for all source files.
    /// </summary>
    private static Dictionary<string, string> ComputeFileHashes(IReadOnlyList<string> sourceFiles)
    {
        return sourceFiles
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(f => f, ComputeFileHash, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Computes a cache key based on source path and dependencies.
    /// </summary>
    private string ComputeCacheKey(string sourcePath, IReadOnlyList<string> sourceFiles)
    {
        var fileHashes = ComputeFileHashes(sourceFiles);
        return ComputeCacheKeyFromHashes(sourcePath, fileHashes);
    }

    /// <summary>
    /// Computes a cache key using pre-computed file hashes.
    /// </summary>
    private string ComputeCacheKeyFromHashes(string sourcePath, Dictionary<string, string> fileHashes)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.AppendLine(_draftSpecVersion);
        keyBuilder.AppendLine(sourcePath);

        // Include all dependency file hashes in sorted order for determinism
        foreach (var (file, hash) in fileHashes.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            keyBuilder.AppendLine($"{file}:{hash}");
        }

        return ComputeHash(keyBuilder.ToString())[..16]; // Use first 16 chars of hash
    }

    /// <summary>
    /// Validates that a cache entry is still valid using pre-computed hashes.
    /// </summary>
    internal bool IsCacheValidWithHashes(CacheMetadata metadata, Dictionary<string, string> currentFileHashes)
    {
        // Check DraftSpec version
        if (metadata.DraftSpecVersion != _draftSpecVersion)
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
    /// Validates that a cache entry is still valid.
    /// </summary>
    internal bool IsCacheValid(CacheMetadata metadata, IReadOnlyList<string> currentSourceFiles)
    {
        // Check DraftSpec version
        if (metadata.DraftSpecVersion != _draftSpecVersion)
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

    /// <summary>
    /// Computes SHA256 hash of a string.
    /// </summary>
    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Computes SHA256 hash of a file's contents.
    /// </summary>
    private static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void EnsureCacheDirectoryExists()
    {
        Directory.CreateDirectory(_cacheDirectory);
    }

    private string GetMetadataPath(string cacheKey) =>
        Path.Combine(_cacheDirectory, cacheKey + MetadataExtension);

    private string GetResultPath(string cacheKey) =>
        Path.Combine(_cacheDirectory, cacheKey + ResultExtension);

    private void DeleteCacheEntry(string cacheKey)
    {
        try
        {
            var metaPath = GetMetadataPath(cacheKey);
            var resultPath = GetResultPath(cacheKey);

            if (File.Exists(metaPath)) File.Delete(metaPath);
            if (File.Exists(resultPath)) File.Delete(resultPath);
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    private static async Task<CacheMetadata?> LoadMetadataAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<CacheMetadata>(stream, JsonOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    private static async Task SaveMetadataAsync(string path, CacheMetadata metadata, CancellationToken ct)
    {
        var tempPath = path + ".tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await JsonSerializer.SerializeAsync(stream, metadata, JsonOptions, ct);
        }
        File.Move(tempPath, path, overwrite: true);
    }

    private static async Task<StaticParseResult?> LoadResultAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var cached = await JsonSerializer.DeserializeAsync<CachedResult>(stream, JsonOptions, ct);
            if (cached == null)
                return null;

            return new StaticParseResult
            {
                Specs = cached.Specs.Select(s => new StaticSpec
                {
                    Description = s.Description,
                    ContextPath = s.ContextPath,
                    LineNumber = s.LineNumber,
                    Type = s.Type,
                    IsPending = s.IsPending
                }).ToList(),
                Warnings = cached.Warnings,
                IsComplete = cached.IsComplete
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task SaveResultAsync(string path, CachedResult result, CancellationToken ct)
    {
        var tempPath = path + ".tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await JsonSerializer.SerializeAsync(stream, result, JsonOptions, ct);
        }
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>
    /// Metadata stored alongside cached results.
    /// </summary>
    internal sealed class CacheMetadata
    {
        public string CacheKey { get; set; } = "";
        public string SourcePath { get; set; } = "";
        public List<string> SourceFiles { get; set; } = [];
        public Dictionary<string, string> SourceFileHashes { get; set; } = [];
        public string DraftSpecVersion { get; set; } = "";
        public DateTime CachedAtUtc { get; set; }
    }

    /// <summary>
    /// Serializable format for cached parse results.
    /// </summary>
    private sealed class CachedResult
    {
        public List<CachedSpec> Specs { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
        public bool IsComplete { get; set; }
    }

    /// <summary>
    /// Serializable format for cached specs.
    /// </summary>
    private sealed class CachedSpec
    {
        public string Description { get; set; } = "";
        public List<string> ContextPath { get; set; } = [];
        public int LineNumber { get; set; }
        public StaticSpecType Type { get; set; }
        public bool IsPending { get; set; }
    }
}

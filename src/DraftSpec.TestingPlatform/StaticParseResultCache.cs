using System.Text.Json;
using System.Text.Json.Serialization;
using DraftSpec.Scripting;
using Microsoft.Extensions.Logging;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Disk-based cache for static parse results.
/// Stores parsed spec structures to avoid re-parsing unchanged files.
/// </summary>
public sealed class StaticParseResultCache : DiskCacheBase
{
    private const string CacheDirectoryName = ".draftspec";
    private const string ParsingCacheSubdirectory = "cache/parsing";
    private const string ResultExtension = ".result.json";

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
    /// <param name="logger">Optional logger for cache operations.</param>
    public StaticParseResultCache(string projectDirectory, ILogger? logger = null)
        : base(
            Path.Combine(projectDirectory, CacheDirectoryName, ParsingCacheSubdirectory),
            typeof(StaticParseResult).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            logger)
    {
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

            var cacheKey = ComputeCacheKeyFromHashes(sourcePath, fileHashes, additionalContent: null);
            var metadataPath = GetMetadataPath(cacheKey);

            if (!File.Exists(metadataPath))
                return (false, null);

            var metadata = await LoadMetadataAsync(metadataPath, cancellationToken).ConfigureAwait(false);
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
            var result = await LoadResultAsync(resultPath, cancellationToken).ConfigureAwait(false);
            if (result == null)
                return (false, null);

            return (true, result);
        }
        catch (Exception ex)
        {
            // Cache read errors should fall back to parsing
            Logger.LogDebug(ex, "Cache read failed for {SourcePath}, falling back to parsing", sourcePath);
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
                DraftSpecVersion = Version,
                CachedAtUtc = DateTime.UtcNow
            };

            await SaveMetadataAsync(GetMetadataPath(cacheKey), metadata, cancellationToken).ConfigureAwait(false);

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

            await SaveResultAsync(GetResultPath(cacheKey), resultData, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Cache write errors should not fail parsing
            Logger.LogDebug(ex, "Failed to cache parse result for {SourcePath}", sourcePath);
        }
    }

    /// <summary>
    /// Clears all cached parse results.
    /// </summary>
    public override void Clear()
    {
        base.Clear();
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return base.GetStatistics(ResultExtension);
    }

    /// <summary>
    /// Validates that a cache entry is still valid using pre-computed hashes.
    /// </summary>
    internal bool IsCacheValidWithHashes(CacheMetadata metadata, Dictionary<string, string> currentFileHashes)
    {
        return ValidateHashesMatch(metadata, currentFileHashes);
    }

    /// <summary>
    /// Validates that a cache entry is still valid.
    /// </summary>
    internal bool IsCacheValid(CacheMetadata metadata, IReadOnlyList<string> currentSourceFiles)
    {
        return ValidateFilesUnchanged(metadata, currentSourceFiles);
    }

    private string GetResultPath(string cacheKey) =>
        GetFilePath(cacheKey, ResultExtension);

    private void DeleteCacheEntry(string cacheKey)
    {
        DeleteFiles(cacheKey, ResultExtension);
    }

    private async Task<CacheMetadata?> LoadMetadataAsync(string path, CancellationToken ct)
    {
        return await LoadJsonAsync<CacheMetadata>(path, JsonOptions, ct).ConfigureAwait(false);
    }

    private static async Task SaveMetadataAsync(string path, CacheMetadata metadata, CancellationToken ct)
    {
        await AtomicWriteJsonAsync(path, metadata, JsonOptions, ct).ConfigureAwait(false);
    }

    private async Task<StaticParseResult?> LoadResultAsync(string path, CancellationToken ct)
    {
        try
        {
            var stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                var cached = await JsonSerializer.DeserializeAsync<CachedResult>(stream, JsonOptions, ct).ConfigureAwait(false);
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
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to load result from {Path}", path);
            return null;
        }
    }

    private static async Task SaveResultAsync(string path, CachedResult result, CancellationToken ct)
    {
        await AtomicWriteJsonAsync(path, result, JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Metadata stored alongside cached results.
    /// </summary>
    internal sealed class CacheMetadata : ICacheMetadata
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

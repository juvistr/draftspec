using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Scripting;

/// <summary>
/// Disk-based cache for compiled CSX scripts.
/// Stores compiled assemblies and metadata to avoid recompilation on subsequent runs.
/// </summary>
public sealed class ScriptCompilationCache : DiskCacheBase
{
    private const string CacheDirectoryName = ".draftspec";
    private const string ScriptsCacheSubdirectory = "cache/scripts";
    private const string AssemblyExtension = ".dll";
    private const string PdbExtension = ".pdb";

    /// <summary>
    /// JSON serialization options for cache metadata.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Creates a new script compilation cache.
    /// </summary>
    /// <param name="projectDirectory">The project directory containing .draftspec folder.</param>
    /// <param name="logger">Optional logger for cache operations. Debug level logs cache hits/misses and errors.</param>
    public ScriptCompilationCache(string projectDirectory, ILogger? logger = null)
        : base(
            Path.Combine(projectDirectory, CacheDirectoryName, ScriptsCacheSubdirectory),
            typeof(Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            logger)
    {
    }

    /// <summary>
    /// Tries to execute a cached script directly from the cached assembly.
    /// </summary>
    /// <param name="sourcePath">Absolute path to the main source file.</param>
    /// <param name="sourceFiles">All source files including #load dependencies.</param>
    /// <param name="preprocessedCode">The preprocessed combined source code.</param>
    /// <param name="globals">The script globals to pass.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True and the result if cache hit, false otherwise.</returns>
    public async Task<(bool success, object? result)> TryExecuteCachedAsync(
        string sourcePath,
        IReadOnlyList<string> sourceFiles,
        string preprocessedCode,
        ScriptGlobals globals,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Compute file hashes once and reuse for both cache key and validation
            var fileHashes = ComputeFileHashes(sourceFiles);

            var cacheKey = ComputeCacheKeyFromHashes(sourcePath, fileHashes, preprocessedCode);
            var metadataPath = GetMetadataPath(cacheKey);

            if (!File.Exists(metadataPath))
                return (false, null);

            var metadata = LoadMetadata(metadataPath);
            if (metadata == null)
                return (false, null);

            // Validate cache using pre-computed hashes
            if (!IsCacheValidWithHashes(metadata, fileHashes))
            {
                // Remove stale cache files
                DeleteCacheEntry(cacheKey);
                return (false, null);
            }

            var assemblyPath = GetAssemblyPath(cacheKey);
            if (!File.Exists(assemblyPath))
                return (false, null);

            // Load and execute the cached assembly
            var result = await ExecuteCachedAssemblyAsync(assemblyPath, globals, cancellationToken);
            return (true, result);
        }
        catch (Exception ex)
        {
            // Cache read/execute errors should fall back to normal compilation
            Logger.LogDebug(ex, "Cache read/execute failed for {SourcePath}, falling back to compilation", sourcePath);
            return (false, null);
        }
    }

    /// <summary>
    /// Loads and executes a cached script assembly.
    /// </summary>
    internal static async Task<object?> ExecuteCachedAssemblyAsync(
        string assemblyPath,
        ScriptGlobals globals,
        CancellationToken cancellationToken)
    {
        // Load assembly into a collectible context for proper unloading
        var context = new AssemblyLoadContext(name: null, isCollectible: true);
        try
        {
            var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));

            // Find the submission type (Roslyn generates Submission#0)
            var submissionType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.StartsWith("Submission#", StringComparison.Ordinal))
                ?? throw new InvalidOperationException("Could not find submission type in cached assembly");

            // Find the factory method (Roslyn generates <Factory>)
            var factoryMethod = submissionType.GetMethod(
                "<Factory>",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find factory method in cached assembly");

            // Invoke the factory method with globals
            var parameters = new object?[] { new object?[] { globals, null } };
            var task = (Task<object>)factoryMethod.Invoke(null, parameters)!;

            cancellationToken.ThrowIfCancellationRequested();
            return await task;
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    /// Caches a compiled script for future use.
    /// </summary>
    /// <param name="sourcePath">Absolute path to the main source file.</param>
    /// <param name="sourceFiles">All source files including #load dependencies.</param>
    /// <param name="preprocessedCode">The preprocessed combined source code.</param>
    /// <param name="script">The compiled script to cache.</param>
    public void CacheScript(
        string sourcePath,
        IReadOnlyList<string> sourceFiles,
        string preprocessedCode,
        Script<object> script)
    {
        try
        {
            EnsureCacheDirectoryExists();

            var cacheKey = ComputeCacheKey(sourcePath, sourceFiles, preprocessedCode);
            var compilation = script.GetCompilation();

            // Emit the compiled assembly to disk
            var assemblyPath = GetAssemblyPath(cacheKey);
            var pdbPath = GetPdbPath(cacheKey);

            using (var assemblyStream = new FileStream(assemblyPath, FileMode.Create))
            using (var pdbStream = new FileStream(pdbPath, FileMode.Create))
            {
                var emitResult = compilation.Emit(assemblyStream, pdbStream);
                if (!emitResult.Success)
                {
                    // Compilation failed - don't cache
                    assemblyStream.Close();
                    pdbStream.Close();
                    DeleteCacheEntry(cacheKey);
                    return;
                }
            }

            // Save metadata
            var metadata = new CacheMetadata
            {
                CacheKey = cacheKey,
                SourcePath = sourcePath,
                SourceFiles = sourceFiles.ToList(),
                SourceFileHashes = sourceFiles.ToDictionary(f => f, ComputeFileHash),
                ContentHash = ComputeHash(preprocessedCode),
                DraftSpecVersion = Version,
                CachedAtUtc = DateTime.UtcNow,
                MaxModifiedTimeUtc = GetMaxModificationTime(sourceFiles)
            };

            SaveMetadata(GetMetadataPath(cacheKey), metadata);
        }
        catch (Exception ex)
        {
            // Cache write errors should not fail the build
            Logger.LogDebug(ex, "Failed to cache compiled script for {SourcePath}", sourcePath);
        }
    }

    /// <summary>
    /// Clears all cached scripts.
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
        return base.GetStatistics(AssemblyExtension);
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

    /// <summary>
    /// Gets the maximum modification time from a list of files.
    /// </summary>
    private static DateTime GetMaxModificationTime(IReadOnlyList<string> files)
    {
        return files.Max(f => File.GetLastWriteTimeUtc(f));
    }

    private string GetAssemblyPath(string cacheKey) =>
        GetFilePath(cacheKey, AssemblyExtension);

    private string GetPdbPath(string cacheKey) =>
        GetFilePath(cacheKey, PdbExtension);

    private void DeleteCacheEntry(string cacheKey)
    {
        DeleteFiles(cacheKey, AssemblyExtension, PdbExtension);
    }

    private CacheMetadata? LoadMetadata(string path)
    {
        return LoadJson<CacheMetadata>(path, JsonOptions);
    }

    private static void SaveMetadata(string path, CacheMetadata metadata)
    {
        AtomicWriteJson(path, metadata, JsonOptions);
    }

    /// <summary>
    /// Metadata stored alongside cached assemblies.
    /// </summary>
    internal sealed class CacheMetadata : ICacheMetadata
    {
        public string CacheKey { get; set; } = "";
        public string SourcePath { get; set; } = "";
        public List<string> SourceFiles { get; set; } = [];
        public Dictionary<string, string> SourceFileHashes { get; set; } = [];
        public string ContentHash { get; set; } = "";
        public string DraftSpecVersion { get; set; } = "";
        public DateTime CachedAtUtc { get; set; }
        public DateTime MaxModifiedTimeUtc { get; set; }
    }
}

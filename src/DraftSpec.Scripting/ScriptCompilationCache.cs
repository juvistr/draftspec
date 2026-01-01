using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Scripting;

/// <summary>
/// Disk-based cache for compiled CSX scripts.
/// Stores compiled assemblies and metadata to avoid recompilation on subsequent runs.
/// </summary>
public sealed class ScriptCompilationCache
{
    private const string CacheDirectoryName = ".draftspec";
    private const string ScriptsCacheSubdirectory = "cache/scripts";
    private const string MetadataExtension = ".meta.json";
    private const string AssemblyExtension = ".dll";
    private const string PdbExtension = ".pdb";

    private readonly string _cacheDirectory;
    private readonly string _draftSpecVersion;

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
    public ScriptCompilationCache(string projectDirectory)
    {
        _cacheDirectory = Path.Combine(projectDirectory, CacheDirectoryName, ScriptsCacheSubdirectory);
        _draftSpecVersion = typeof(Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0";
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
        catch
        {
            // Cache read/execute errors should fall back to normal compilation
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
                DraftSpecVersion = _draftSpecVersion,
                CachedAtUtc = DateTime.UtcNow,
                MaxModifiedTimeUtc = GetMaxModificationTime(sourceFiles)
            };

            SaveMetadata(GetMetadataPath(cacheKey), metadata);
        }
        catch
        {
            // Cache write errors should not fail the build
        }
    }

    /// <summary>
    /// Clears all cached scripts.
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
            var dllFiles = Directory.GetFiles(_cacheDirectory, "*" + AssemblyExtension);
            var totalSize = dllFiles.Sum(f => new FileInfo(f).Length);

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
    /// Computes a cache key based on source path, dependencies, and content.
    /// </summary>
    private string ComputeCacheKey(string sourcePath, IReadOnlyList<string> sourceFiles, string preprocessedCode)
    {
        var fileHashes = ComputeFileHashes(sourceFiles);
        return ComputeCacheKeyFromHashes(sourcePath, fileHashes, preprocessedCode);
    }

    /// <summary>
    /// Computes a cache key using pre-computed file hashes.
    /// </summary>
    private string ComputeCacheKeyFromHashes(
        string sourcePath,
        Dictionary<string, string> fileHashes,
        string preprocessedCode)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.AppendLine(_draftSpecVersion);
        keyBuilder.AppendLine(sourcePath);

        // Include all dependency file hashes in sorted order for determinism
        foreach (var (file, hash) in fileHashes.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            keyBuilder.AppendLine($"{file}:{hash}");
        }

        keyBuilder.AppendLine(ComputeHash(preprocessedCode));

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

    /// <summary>
    /// Gets the maximum modification time from a list of files.
    /// </summary>
    private static DateTime GetMaxModificationTime(IReadOnlyList<string> files)
    {
        return files.Max(f => File.GetLastWriteTimeUtc(f));
    }

    private void EnsureCacheDirectoryExists()
    {
        Directory.CreateDirectory(_cacheDirectory);
    }

    private string GetMetadataPath(string cacheKey) =>
        Path.Combine(_cacheDirectory, cacheKey + MetadataExtension);

    private string GetAssemblyPath(string cacheKey) =>
        Path.Combine(_cacheDirectory, cacheKey + AssemblyExtension);

    private string GetPdbPath(string cacheKey) =>
        Path.Combine(_cacheDirectory, cacheKey + PdbExtension);

    private void DeleteCacheEntry(string cacheKey)
    {
        try
        {
            var metaPath = GetMetadataPath(cacheKey);
            var dllPath = GetAssemblyPath(cacheKey);
            var pdbPath = GetPdbPath(cacheKey);

            if (File.Exists(metaPath)) File.Delete(metaPath);
            if (File.Exists(dllPath)) File.Delete(dllPath);
            if (File.Exists(pdbPath)) File.Delete(pdbPath);
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    private static CacheMetadata? LoadMetadata(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CacheMetadata>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void SaveMetadata(string path, CacheMetadata metadata)
    {
        var tempPath = path + ".tmp";
        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>
    /// Metadata stored alongside cached assemblies.
    /// </summary>
    internal sealed class CacheMetadata
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


using System.Security.Cryptography;
using System.Text;
using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for caching behavior.
/// These tests verify cache key determinism and hash properties.
/// </summary>
public class CachingPropertyTests
{
    [Test]
    public void SHA256Hash_IsDeterministic()
    {
        // Property: Same input always produces same hash
        Prop.ForAll<NonNull<string>>(input =>
        {
            var hash1 = ComputeHash(input.Get);
            var hash2 = ComputeHash(input.Get);

            return hash1 == hash2;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void SHA256Hash_DifferentInputsProduceDifferentHashes()
    {
        // Property: Different inputs produce different hashes (with high probability)
        Prop.ForAll<NonNull<string>, NonNull<string>>((input1, input2) =>
        {
            if (input1.Get == input2.Get)
                return true; // Skip identical inputs

            var hash1 = ComputeHash(input1.Get);
            var hash2 = ComputeHash(input2.Get);

            return hash1 != hash2;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void SHA256Hash_IsConsistentLength()
    {
        // Property: SHA256 hash is always 64 hex characters
        Prop.ForAll<NonNull<string>>(input =>
        {
            var hash = ComputeHash(input.Get);
            return hash.Length == 64;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void SHA256Hash_IsHexadecimal()
    {
        // Property: SHA256 hash contains only hexadecimal characters
        Prop.ForAll<NonNull<string>>(input =>
        {
            var hash = ComputeHash(input.Get);
            return hash.All(c => "0123456789abcdef".Contains(c));
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task CacheKey_SortedFilesProduceSameKey()
    {
        // Property: File order doesn't affect cache key (sorted internally)
        var files1 = new[] { "a.cs", "b.cs", "c.cs" };
        var files2 = new[] { "c.cs", "a.cs", "b.cs" };

        var key1 = ComputeMockCacheKey("source.csx", files1, "code");
        var key2 = ComputeMockCacheKey("source.csx", files2, "code");

        await Assert.That(key1).IsEqualTo(key2);
    }

    [Test]
    public void CacheKey_DifferentSourcePathProducesDifferentKey()
    {
        // Property: Different source paths produce different cache keys
        Prop.ForAll<NonNull<string>, NonNull<string>>((path1, path2) =>
        {
            if (path1.Get == path2.Get)
                return true;

            var key1 = ComputeMockCacheKey(path1.Get, Array.Empty<string>(), "code");
            var key2 = ComputeMockCacheKey(path2.Get, Array.Empty<string>(), "code");

            return key1 != key2;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void CacheKey_DifferentCodeProducesDifferentKey()
    {
        // Property: Different preprocessed code produces different cache keys
        Prop.ForAll<NonNull<string>, NonNull<string>>((code1, code2) =>
        {
            if (code1.Get == code2.Get)
                return true;

            var key1 = ComputeMockCacheKey("source.csx", Array.Empty<string>(), code1.Get);
            var key2 = ComputeMockCacheKey("source.csx", Array.Empty<string>(), code2.Get);

            return key1 != key2;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void CacheKey_HasFixedLength()
    {
        // Property: Cache key is truncated to fixed length (16 characters)
        Prop.ForAll<NonNull<string>>(code =>
        {
            var key = ComputeMockCacheKey("source.csx", Array.Empty<string>(), code.Get);
            return key.Length == 16;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void CacheKey_IsDeterministic()
    {
        // Property: Same inputs always produce same cache key
        Prop.ForAll<NonNull<string>>((code) =>
        {
            var files = new[] { "file1.cs", "file2.cs" };

            var key1 = ComputeMockCacheKey("source.csx", files, code.Get);
            var key2 = ComputeMockCacheKey("source.csx", files, code.Get);

            return key1 == key2;
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Compute SHA256 hash of a string (mirrors ScriptCompilationCache.ComputeHash).
    /// </summary>
    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Simplified mock of cache key computation for testing.
    /// Mirrors the logic in ScriptCompilationCache.ComputeCacheKey without file I/O.
    /// </summary>
    private static string ComputeMockCacheKey(string sourcePath, IEnumerable<string> sourceFiles, string preprocessedCode)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.AppendLine("1.0.0"); // Mock version
        keyBuilder.AppendLine(sourcePath);

        // Include files in sorted order for determinism
        foreach (var file in sourceFiles.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            // Use file path itself as mock hash (real impl uses file content hash)
            keyBuilder.AppendLine($"{file}:{ComputeHash(file)}");
        }

        keyBuilder.AppendLine(ComputeHash(preprocessedCode));

        return ComputeHash(keyBuilder.ToString())[..16];
    }
}

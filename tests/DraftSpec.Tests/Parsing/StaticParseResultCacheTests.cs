using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Parsing;

/// <summary>
/// Tests for StaticParseResultCache disk-based caching.
/// </summary>
public class StaticParseResultCacheTests
{
    private readonly string _testDir;
    private readonly StaticParseResultCache _cache;

    public StaticParseResultCacheTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"draftspec-parse-cache-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _cache = new StaticParseResultCache(_testDir);
    }

    [After(Test)]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    public async Task CacheAsync_CreatesMetadataAndResultFiles()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "test.csx");
        await File.WriteAllTextAsync(sourceFile, "describe(\"test\", () => it(\"works\", () => {}));");

        var result = new StaticParseResult
        {
            Specs = [new StaticSpec
            {
                Description = "works",
                ContextPath = ["test"],
                LineNumber = 1,
                Type = StaticSpecType.Regular,
                IsPending = false
            }],
            Warnings = [],
            IsComplete = true
        };

        // Act
        await _cache.CacheAsync(sourceFile, [sourceFile], result);

        // Assert
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "parsing");
        var metaFiles = Directory.GetFiles(cacheDir, "*.meta.json");
        var resultFiles = Directory.GetFiles(cacheDir, "*.result.json");

        await Assert.That(metaFiles.Length).IsEqualTo(1);
        await Assert.That(resultFiles.Length).IsEqualTo(1);
    }

    [Test]
    public async Task TryGetCachedAsync_ReturnsFalse_WhenNotCached()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "uncached.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Act
        var (success, _) = await _cache.TryGetCachedAsync(sourceFile, [sourceFile]);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryGetCachedAsync_ReturnsTrue_WhenCached()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "cached.csx");
        await File.WriteAllTextAsync(sourceFile, "describe(\"test\", () => it(\"works\", () => {}));");

        var result = new StaticParseResult
        {
            Specs = [new StaticSpec
            {
                Description = "works",
                ContextPath = ["test"],
                LineNumber = 1,
                Type = StaticSpecType.Regular,
                IsPending = false
            }],
            Warnings = [],
            IsComplete = true
        };

        await _cache.CacheAsync(sourceFile, [sourceFile], result);

        // Act
        var (success, cached) = await _cache.TryGetCachedAsync(sourceFile, [sourceFile]);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(cached).IsNotNull();
        await Assert.That(cached!.Specs).Count().IsEqualTo(1);
        await Assert.That(cached.Specs[0].Description).IsEqualTo("works");
    }

    [Test]
    public async Task TryGetCachedAsync_ReturnsFalse_WhenSourceChanged()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "changing.csx");
        await File.WriteAllTextAsync(sourceFile, "describe(\"v1\", () => {});");

        var result = new StaticParseResult { Specs = [], Warnings = [], IsComplete = true };
        await _cache.CacheAsync(sourceFile, [sourceFile], result);

        // Modify the source
        await File.WriteAllTextAsync(sourceFile, "describe(\"v2\", () => {});");

        // Act
        var (success, _) = await _cache.TryGetCachedAsync(sourceFile, [sourceFile]);

        // Assert - should miss because hash changed
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task GetStatistics_ReturnsCorrectCounts()
    {
        // Arrange
        var sourceFile1 = Path.Combine(_testDir, "test1.csx");
        var sourceFile2 = Path.Combine(_testDir, "test2.csx");
        await File.WriteAllTextAsync(sourceFile1, "var x = 1;");
        await File.WriteAllTextAsync(sourceFile2, "var y = 2;");

        var result = new StaticParseResult { Specs = [], Warnings = [], IsComplete = true };
        await _cache.CacheAsync(sourceFile1, [sourceFile1], result);
        await _cache.CacheAsync(sourceFile2, [sourceFile2], result);

        // Act
        var stats = _cache.GetStatistics();

        // Assert
        await Assert.That(stats.EntryCount).IsEqualTo(2);
        await Assert.That(stats.TotalSizeBytes).IsGreaterThan(0);
    }

    [Test]
    public async Task Clear_RemovesAllCachedEntries()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "to-clear.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var result = new StaticParseResult { Specs = [], Warnings = [], IsComplete = true };
        await _cache.CacheAsync(sourceFile, [sourceFile], result);

        var statsBefore = _cache.GetStatistics();
        await Assert.That(statsBefore.EntryCount).IsEqualTo(1);

        // Act
        _cache.Clear();

        // Assert
        var statsAfter = _cache.GetStatistics();
        await Assert.That(statsAfter.EntryCount).IsEqualTo(0);
    }

    [Test]
    public async Task TryGetCachedAsync_ReturnsFalse_WhenSourceFileMissing()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "missing.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var result = new StaticParseResult { Specs = [], Warnings = [], IsComplete = true };
        await _cache.CacheAsync(sourceFile, [sourceFile], result);

        // Delete the source file
        File.Delete(sourceFile);

        // Act
        var (success, _) = await _cache.TryGetCachedAsync(sourceFile, [sourceFile]);

        // Assert - should miss because source file is gone
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryGetCachedAsync_ReturnsFalse_WhenFileCountDiffers()
    {
        // Arrange
        var sourceFile1 = Path.Combine(_testDir, "file1.csx");
        var sourceFile2 = Path.Combine(_testDir, "file2.csx");
        await File.WriteAllTextAsync(sourceFile1, "var x = 1;");
        await File.WriteAllTextAsync(sourceFile2, "var y = 2;");

        var result = new StaticParseResult { Specs = [], Warnings = [], IsComplete = true };
        // Cache with one file
        await _cache.CacheAsync(sourceFile1, [sourceFile1], result);

        // Act - try with two files (different count)
        var (success, _) = await _cache.TryGetCachedAsync(sourceFile1, [sourceFile1, sourceFile2]);

        // Assert - should miss because file count changed
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task CacheAsync_PreservesSpecDetails()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "details.csx");
        await File.WriteAllTextAsync(sourceFile, "fit(\"focused\");");

        var result = new StaticParseResult
        {
            Specs = [new StaticSpec
            {
                Description = "focused spec",
                ContextPath = ["outer", "inner"],
                LineNumber = 42,
                Type = StaticSpecType.Focused,
                IsPending = true
            }],
            Warnings = ["Warning 1", "Warning 2"],
            IsComplete = false
        };

        // Act
        await _cache.CacheAsync(sourceFile, [sourceFile], result);
        var (success, cached) = await _cache.TryGetCachedAsync(sourceFile, [sourceFile]);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(cached!.Specs[0].Description).IsEqualTo("focused spec");
        await Assert.That(cached.Specs[0].ContextPath).Contains("outer");
        await Assert.That(cached.Specs[0].ContextPath).Contains("inner");
        await Assert.That(cached.Specs[0].LineNumber).IsEqualTo(42);
        await Assert.That(cached.Specs[0].Type).IsEqualTo(StaticSpecType.Focused);
        await Assert.That(cached.Specs[0].IsPending).IsTrue();
        await Assert.That(cached.Warnings).Count().IsEqualTo(2);
        await Assert.That(cached.IsComplete).IsFalse();
    }

    [Test]
    public async Task IsCacheValid_ReturnsFalse_WhenVersionMismatch()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "version-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var metadata = new StaticParseResultCache.CacheMetadata
        {
            DraftSpecVersion = "0.0.0-wrong-version",
            SourceFiles = [sourceFile],
            SourceFileHashes = new Dictionary<string, string> { [sourceFile] = "somehash" }
        };

        // Act
        var result = _cache.IsCacheValid(metadata, [sourceFile]);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCacheValid_ReturnsFalse_WhenFileNotInCachedHashes()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "not-in-hash.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var metadata = new StaticParseResultCache.CacheMetadata
        {
            DraftSpecVersion = typeof(StaticParseResult).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            SourceFiles = [sourceFile],
            SourceFileHashes = new Dictionary<string, string>() // Empty - file not in hashes
        };

        // Act
        var result = _cache.IsCacheValid(metadata, [sourceFile]);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCacheValid_ReturnsFalse_WhenHashMismatch()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "hash-mismatch.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var metadata = new StaticParseResultCache.CacheMetadata
        {
            DraftSpecVersion = typeof(StaticParseResult).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            SourceFiles = [sourceFile],
            SourceFileHashes = new Dictionary<string, string> { [sourceFile] = "wrong-hash-value" }
        };

        // Act
        var result = _cache.IsCacheValid(metadata, [sourceFile]);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task StaticSpecParser_WithCache_UsesCache()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "parser-cache.csx");
        await File.WriteAllTextAsync(sourceFile, @"
using static DraftSpec.Dsl;
describe(""parser cache test"", () => it(""works"", () => {}));
");

        var parser = new StaticSpecParser(_testDir, useCache: true);

        // Act - First parse (populates cache)
        var result1 = await parser.ParseFileAsync(sourceFile);

        // Verify cache was created
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "parsing");
        await Assert.That(Directory.Exists(cacheDir)).IsTrue();
        var resultFiles = Directory.GetFiles(cacheDir, "*.result.json");
        await Assert.That(resultFiles.Length).IsEqualTo(1);

        // Act - Second parse (should use cache)
        var result2 = await parser.ParseFileAsync(sourceFile);

        // Assert - both should return same results
        await Assert.That(result1.Specs).Count().IsEqualTo(1);
        await Assert.That(result2.Specs).Count().IsEqualTo(1);
        await Assert.That(result1.Specs[0].Description).IsEqualTo("works");
        await Assert.That(result2.Specs[0].Description).IsEqualTo("works");
    }

    [Test]
    public async Task StaticSpecParser_WithCacheDisabled_DoesNotCreateCache()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "no-cache.csx");
        await File.WriteAllTextAsync(sourceFile, @"
using static DraftSpec.Dsl;
describe(""no cache test"", () => it(""works"", () => {}));
");

        var parser = new StaticSpecParser(_testDir, useCache: false);

        // Act
        await parser.ParseFileAsync(sourceFile);

        // Assert - cache directory should not exist
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "parsing");
        await Assert.That(Directory.Exists(cacheDir)).IsFalse();
    }
}

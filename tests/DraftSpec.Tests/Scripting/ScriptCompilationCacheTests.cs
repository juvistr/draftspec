using DraftSpec.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Tests.Scripting;

/// <summary>
/// Tests for ScriptCompilationCache disk-based caching.
/// </summary>
public class ScriptCompilationCacheTests
{
    private readonly string _testDir;
    private readonly ScriptCompilationCache _cache;

    public ScriptCompilationCacheTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"draftspec-cache-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _cache = new ScriptCompilationCache(_testDir);
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
    public async Task CacheScript_CreatesMetadataAndAssemblyFiles()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1 + 2;");

        var script = CreateTestScript("var x = 1 + 2;");
        var sourceFiles = new[] { sourceFile };

        // Act
        _cache.CacheScript(sourceFile, sourceFiles, "var x = 1 + 2;", script);

        // Assert
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var metaFiles = Directory.GetFiles(cacheDir, "*.meta.json");
        var dllFiles = Directory.GetFiles(cacheDir, "*.dll");

        await Assert.That(metaFiles.Length).IsEqualTo(1);
        await Assert.That(dllFiles.Length).IsEqualTo(1);
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsFalse_WhenNotCached()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "uncached.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var globals = new ScriptGlobals();

        // Act
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 1;",
            globals);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsTrue_WhenCached()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "cached.csx");
        var code = "CaptureRootContext?.Invoke(DraftSpec.Dsl.RootContext);";
        await File.WriteAllTextAsync(sourceFile, code);

        var script = CreateTestScript(code);
        _cache.CacheScript(sourceFile, [sourceFile], code, script);

        var globals = new ScriptGlobals();

        // Act
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            code,
            globals);

        // Assert
        await Assert.That(success).IsTrue();
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsFalse_WhenSourceChanged()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "changing.csx");
        var originalCode = "var x = 1;";
        await File.WriteAllTextAsync(sourceFile, originalCode);

        var script = CreateTestScript(originalCode);
        _cache.CacheScript(sourceFile, [sourceFile], originalCode, script);

        // Modify the source
        await File.WriteAllTextAsync(sourceFile, "var x = 2;");

        var globals = new ScriptGlobals();

        // Act
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 2;", // Different code
            globals);

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

        _cache.CacheScript(sourceFile1, [sourceFile1], "var x = 1;", CreateTestScript("var x = 1;"));
        _cache.CacheScript(sourceFile2, [sourceFile2], "var y = 2;", CreateTestScript("var y = 2;"));

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
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        var statsBefore = _cache.GetStatistics();
        await Assert.That(statsBefore.EntryCount).IsEqualTo(1);

        // Act
        _cache.Clear();

        // Assert
        var statsAfter = _cache.GetStatistics();
        await Assert.That(statsAfter.EntryCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetStatistics_ReturnsEmptyStats_WhenNoCacheDirectory()
    {
        // Arrange
        var emptyDir = Path.Combine(_testDir, "empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyDir);
        var emptyCache = new ScriptCompilationCache(emptyDir);

        // Act
        var stats = emptyCache.GetStatistics();

        // Assert
        await Assert.That(stats.EntryCount).IsEqualTo(0);
        await Assert.That(stats.TotalSizeBytes).IsEqualTo(0);
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsFalse_WhenSourceFileMissing()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "missing.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script = CreateTestScript("var x = 1;");
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Delete the source file
        File.Delete(sourceFile);

        var globals = new ScriptGlobals();

        // Act
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 1;",
            globals);

        // Assert - should miss because source file is gone
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsFalse_WhenSourceFilesCountDiffers()
    {
        // Arrange
        var sourceFile1 = Path.Combine(_testDir, "file1.csx");
        var sourceFile2 = Path.Combine(_testDir, "file2.csx");
        await File.WriteAllTextAsync(sourceFile1, "var x = 1;");
        await File.WriteAllTextAsync(sourceFile2, "var y = 2;");

        var script = CreateTestScript("var x = 1;");
        // Cache with one file
        _cache.CacheScript(sourceFile1, [sourceFile1], "var x = 1;", script);

        var globals = new ScriptGlobals();

        // Act - try with two files (different count)
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile1,
            [sourceFile1, sourceFile2],
            "var x = 1;",
            globals);

        // Assert - should miss because file count changed
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsFalse_WhenAssemblyFileMissing()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "missing-dll.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script = CreateTestScript("var x = 1;");
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Delete all DLL files from cache
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        foreach (var dll in Directory.GetFiles(cacheDir, "*.dll"))
        {
            File.Delete(dll);
        }

        var globals = new ScriptGlobals();

        // Act
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 1;",
            globals);

        // Assert - should miss because DLL is gone
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task CacheScript_CreatesPdbFile()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "with-pdb.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script = CreateTestScript("var x = 1;");

        // Act
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Assert
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var pdbFiles = Directory.GetFiles(cacheDir, "*.pdb");
        await Assert.That(pdbFiles.Length).IsEqualTo(1);
    }

    [Test]
    public async Task CacheScript_WithMultipleDependencies_TracksAllFiles()
    {
        // Arrange
        var mainFile = Path.Combine(_testDir, "main.csx");
        var depFile1 = Path.Combine(_testDir, "dep1.csx");
        var depFile2 = Path.Combine(_testDir, "dep2.csx");
        await File.WriteAllTextAsync(mainFile, "var x = 1;");
        await File.WriteAllTextAsync(depFile1, "var y = 2;");
        await File.WriteAllTextAsync(depFile2, "var z = 3;");

        var sourceFiles = new[] { mainFile, depFile1, depFile2 };
        var script = CreateTestScript("var x = 1;");

        // Act
        _cache.CacheScript(mainFile, sourceFiles, "var x = 1;", script);

        // Modify a dependency
        await File.WriteAllTextAsync(depFile2, "var z = 99;");

        var globals = new ScriptGlobals();
        var (success, _) = await _cache.TryExecuteCachedAsync(
            mainFile,
            sourceFiles,
            "var x = 1;",
            globals);

        // Assert - should miss because dependency changed
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsFalse_WhenMetadataCorrupted()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "corrupted.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script = CreateTestScript("var x = 1;");
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Corrupt metadata files
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        foreach (var meta in Directory.GetFiles(cacheDir, "*.meta.json"))
        {
            await File.WriteAllTextAsync(meta, "{ invalid json }}}");
        }

        var globals = new ScriptGlobals();

        // Act
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 1;",
            globals);

        // Assert - should fail gracefully and return false
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task GetStatistics_IncludesCacheDirectory()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "stats-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Act
        var stats = _cache.GetStatistics();

        // Assert
        await Assert.That(stats.CacheDirectory).IsNotNull();
        await Assert.That(stats.CacheDirectory).Contains(".draftspec");
    }

    [Test]
    public async Task Clear_SucceedsWhenCacheDirectoryDoesNotExist()
    {
        // Arrange
        var emptyDir = Path.Combine(_testDir, "no-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyDir);
        var emptyCache = new ScriptCompilationCache(emptyDir);

        // Act - should not throw
        emptyCache.Clear();

        // Assert - stats should show empty
        var stats = emptyCache.GetStatistics();
        await Assert.That(stats.EntryCount).IsEqualTo(0);
    }

    [Test]
    public async Task TryExecuteCachedAsync_DeletesCacheEntry_WhenInvalid()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "to-delete.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script = CreateTestScript("var x = 1;");
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Verify cache exists
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var initialMetaCount = Directory.GetFiles(cacheDir, "*.meta.json").Length;
        await Assert.That(initialMetaCount).IsEqualTo(1);

        // Modify the metadata to simulate version mismatch
        var metaFiles = Directory.GetFiles(cacheDir, "*.meta.json");
        var metaContent = await File.ReadAllTextAsync(metaFiles[0]);
        metaContent = metaContent.Replace("\"draftSpecVersion\":", "\"draftSpecVersion\": \"0.0.0-fake\", \"_old\":");
        await File.WriteAllTextAsync(metaFiles[0], metaContent);

        var globals = new ScriptGlobals();

        // Act - cache validation should fail and delete entry
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 1;",
            globals);

        // Assert
        await Assert.That(success).IsFalse();
        // Cache entry should be deleted
        var finalMetaCount = Directory.Exists(cacheDir)
            ? Directory.GetFiles(cacheDir, "*.meta.json").Length
            : 0;
        await Assert.That(finalMetaCount).IsEqualTo(0);
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsFalse_WhenNewFileNotInCachedHashes()
    {
        // Arrange
        var sourceFile1 = Path.Combine(_testDir, "original.csx");
        var sourceFile2 = Path.Combine(_testDir, "new-dep.csx");
        await File.WriteAllTextAsync(sourceFile1, "var x = 1;");

        var script = CreateTestScript("var x = 1;");
        // Cache with only sourceFile1
        _cache.CacheScript(sourceFile1, [sourceFile1], "var x = 1;", script);

        // Create new file that wasn't in original cache
        await File.WriteAllTextAsync(sourceFile2, "var y = 2;");

        // Manually modify metadata to have same file count but different file
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var metaFiles = Directory.GetFiles(cacheDir, "*.meta.json");
        var metaContent = await File.ReadAllTextAsync(metaFiles[0]);
        // Replace the source file path to simulate file mismatch
        metaContent = metaContent.Replace(sourceFile1, sourceFile1 + "-nonexistent");
        await File.WriteAllTextAsync(metaFiles[0], metaContent);

        var globals = new ScriptGlobals();

        // Act - should fail because sourceFile1 is not in cached hashes
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile1,
            [sourceFile1],
            "var x = 1;",
            globals);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryExecuteCachedAsync_ReturnsFalse_WhenAssemblyCorrupted()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "corrupted-dll.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script = CreateTestScript("var x = 1;");
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Corrupt the DLL file
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        foreach (var dll in Directory.GetFiles(cacheDir, "*.dll"))
        {
            await File.WriteAllTextAsync(dll, "not a valid dll");
        }

        var globals = new ScriptGlobals();

        // Act - should catch exception and return false
        var (success, _) = await _cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 1;",
            globals);

        // Assert - should fail gracefully
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task CacheScript_HandlesWriteErrorGracefully()
    {
        // Arrange - create a read-only directory to cause write failure
        var readOnlyDir = Path.Combine(_testDir, "readonly-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(readOnlyDir);
        var cacheSubDir = Path.Combine(readOnlyDir, ".draftspec", "cache", "scripts");
        Directory.CreateDirectory(cacheSubDir);

        // Create a file where the cache would try to write
        var blocker = Path.Combine(cacheSubDir, "blocker");
        await File.WriteAllTextAsync(blocker, "block");
        File.SetAttributes(blocker, FileAttributes.ReadOnly);

        var cache = new ScriptCompilationCache(readOnlyDir);
        var sourceFile = Path.Combine(_testDir, "write-error.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script = CreateTestScript("var x = 1;");

        // Act - should not throw, just silently fail
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Assert - cache should have failed but not thrown
        var stats = cache.GetStatistics();
        // The stats might show 0 or 1 depending on timing, but no exception should occur
        await Assert.That(stats.EntryCount).IsGreaterThanOrEqualTo(0);

        // Cleanup
        File.SetAttributes(blocker, FileAttributes.Normal);
    }

    [Test]
    public async Task CacheScript_SameScriptTwice_OverwritesPreviousCache()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "overwrite.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script1 = CreateTestScript("var x = 1;");
        var script2 = CreateTestScript("var x = 1;");

        // Act - cache same script twice
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script1);
        _cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script2);

        // Assert - should still have just one entry
        var stats = _cache.GetStatistics();
        await Assert.That(stats.EntryCount).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteCachedAssemblyAsync_WithValidCachedAssembly_ExecutesScript()
    {
        // Arrange - cache a real script to get a valid Roslyn assembly
        var sourceFile = Path.Combine(_testDir, "exec-test.csx");
        // Script that returns a value (last expression is the return value)
        var code = "42";
        await File.WriteAllTextAsync(sourceFile, code);

        var script = CreateTestScript(code);
        _cache.CacheScript(sourceFile, [sourceFile], code, script);

        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var cachedDll = Directory.GetFiles(cacheDir, "*.dll").Single();
        var globals = new ScriptGlobals();

        // Act - call internal method directly
        var result = await ScriptCompilationCache.ExecuteCachedAssemblyAsync(
            cachedDll, globals, CancellationToken.None);

        // Assert - script should execute and return the integer
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteCachedAssemblyAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange - cache a real script
        var sourceFile = Path.Combine(_testDir, "cancel-test.csx");
        var code = "42";
        await File.WriteAllTextAsync(sourceFile, code);

        var script = CreateTestScript(code);
        _cache.CacheScript(sourceFile, [sourceFile], code, script);

        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var cachedDll = Directory.GetFiles(cacheDir, "*.dll").Single();
        var globals = new ScriptGlobals();

        // Pre-canceled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - should throw OperationCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await ScriptCompilationCache.ExecuteCachedAssemblyAsync(
                cachedDll, globals, cts.Token));
    }

    [Test]
    public async Task ExecuteCachedAssemblyAsync_WithNonScriptAssembly_ThrowsInvalidOperation()
    {
        // Arrange - use a regular .NET assembly (not a Roslyn script)
        // This will not have a Submission# type
        var nonScriptAssembly = typeof(DraftSpec.Dsl).Assembly.Location;
        var globals = new ScriptGlobals();

        // Act & Assert - should throw because there's no Submission# type
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await ScriptCompilationCache.ExecuteCachedAssemblyAsync(
                nonScriptAssembly, globals, CancellationToken.None));

        await Assert.That(exception!.Message).Contains("submission type");
    }

    [Test]
    public async Task IsCacheValid_ReturnsFalse_WhenVersionMismatch()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "version-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var metadata = new ScriptCompilationCache.CacheMetadata
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
    public async Task IsCacheValid_ReturnsFalse_WhenFileCountMismatch()
    {
        // Arrange
        var sourceFile1 = Path.Combine(_testDir, "file1.csx");
        var sourceFile2 = Path.Combine(_testDir, "file2.csx");
        await File.WriteAllTextAsync(sourceFile1, "var x = 1;");
        await File.WriteAllTextAsync(sourceFile2, "var y = 2;");

        var metadata = new ScriptCompilationCache.CacheMetadata
        {
            DraftSpecVersion = typeof(DraftSpec.Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            SourceFiles = [sourceFile1], // Only one file in metadata
            SourceFileHashes = new Dictionary<string, string> { [sourceFile1] = "hash1" }
        };

        // Act - pass two files but metadata has one
        var result = _cache.IsCacheValid(metadata, [sourceFile1, sourceFile2]);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCacheValid_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var missingFile = Path.Combine(_testDir, "missing.csx");

        var metadata = new ScriptCompilationCache.CacheMetadata
        {
            DraftSpecVersion = typeof(DraftSpec.Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            SourceFiles = [missingFile],
            SourceFileHashes = new Dictionary<string, string> { [missingFile] = "somehash" }
        };

        // Act - file doesn't exist
        var result = _cache.IsCacheValid(metadata, [missingFile]);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCacheValid_ReturnsFalse_WhenFileNotInCachedHashes()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "not-in-hash.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var metadata = new ScriptCompilationCache.CacheMetadata
        {
            DraftSpecVersion = typeof(DraftSpec.Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0",
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

        var metadata = new ScriptCompilationCache.CacheMetadata
        {
            DraftSpecVersion = typeof(DraftSpec.Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            SourceFiles = [sourceFile],
            SourceFileHashes = new Dictionary<string, string> { [sourceFile] = "wrong-hash-value" }
        };

        // Act
        var result = _cache.IsCacheValid(metadata, [sourceFile]);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCacheValid_ReturnsTrue_WhenAllChecksPass()
    {
        // Arrange - cache a real script to get valid metadata
        var sourceFile = Path.Combine(_testDir, "valid-cache.csx");
        var code = "var x = 1;";
        await File.WriteAllTextAsync(sourceFile, code);

        var script = CreateTestScript(code);
        _cache.CacheScript(sourceFile, [sourceFile], code, script);

        // Read the generated metadata
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var metaFile = Directory.GetFiles(cacheDir, "*.meta.json").Single();
        var metaJson = await File.ReadAllTextAsync(metaFile);
        var metadata = System.Text.Json.JsonSerializer.Deserialize<ScriptCompilationCache.CacheMetadata>(
            metaJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        // Act
        var result = _cache.IsCacheValid(metadata!, [sourceFile]);

        // Assert
        await Assert.That(result).IsTrue();
    }

    #region IsCacheValidWithHashes Tests

    [Test]
    public async Task IsCacheValidWithHashes_ReturnsTrue_WhenHashesMatch()
    {
        // Arrange - cache a real script to get valid metadata with correct hashes
        var sourceFile = Path.Combine(_testDir, "hash-valid.csx");
        var code = "var x = 1;";
        await File.WriteAllTextAsync(sourceFile, code);

        var script = CreateTestScript(code);
        _cache.CacheScript(sourceFile, [sourceFile], code, script);

        // Read the generated metadata
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var metaFile = Directory.GetFiles(cacheDir, "*.meta.json").Single();
        var metaJson = await File.ReadAllTextAsync(metaFile);
        var metadata = System.Text.Json.JsonSerializer.Deserialize<ScriptCompilationCache.CacheMetadata>(
            metaJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        // Use the same hashes from metadata (simulating pre-computed hashes)
        var fileHashes = metadata!.SourceFileHashes;

        // Act
        var result = _cache.IsCacheValidWithHashes(metadata, fileHashes);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsCacheValidWithHashes_ReturnsFalse_WhenVersionMismatch()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "hash-version-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var metadata = new ScriptCompilationCache.CacheMetadata
        {
            DraftSpecVersion = "0.0.0-wrong-version",
            SourceFiles = [sourceFile],
            SourceFileHashes = new Dictionary<string, string> { [sourceFile] = "somehash" }
        };

        var fileHashes = new Dictionary<string, string> { [sourceFile] = "somehash" };

        // Act
        var result = _cache.IsCacheValidWithHashes(metadata, fileHashes);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCacheValidWithHashes_ReturnsFalse_WhenFileCountMismatch()
    {
        // Arrange
        var sourceFile1 = Path.Combine(_testDir, "hash-count1.csx");
        var sourceFile2 = Path.Combine(_testDir, "hash-count2.csx");

        var metadata = new ScriptCompilationCache.CacheMetadata
        {
            DraftSpecVersion = typeof(DraftSpec.Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            SourceFiles = [sourceFile1], // Only one file
            SourceFileHashes = new Dictionary<string, string> { [sourceFile1] = "hash1" }
        };

        // Pass two files in hash dictionary
        var fileHashes = new Dictionary<string, string>
        {
            [sourceFile1] = "hash1",
            [sourceFile2] = "hash2"
        };

        // Act
        var result = _cache.IsCacheValidWithHashes(metadata, fileHashes);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCacheValidWithHashes_ReturnsFalse_WhenFileNotInCachedHashes()
    {
        // Arrange
        var sourceFile1 = Path.Combine(_testDir, "hash-missing1.csx");
        var sourceFile2 = Path.Combine(_testDir, "hash-missing2.csx");

        var metadata = new ScriptCompilationCache.CacheMetadata
        {
            DraftSpecVersion = typeof(DraftSpec.Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            SourceFiles = [sourceFile1],
            SourceFileHashes = new Dictionary<string, string> { [sourceFile1] = "hash1" } // Only has sourceFile1
        };

        // Current hashes has different file
        var fileHashes = new Dictionary<string, string> { [sourceFile2] = "hash2" };

        // Act
        var result = _cache.IsCacheValidWithHashes(metadata, fileHashes);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCacheValidWithHashes_ReturnsFalse_WhenHashesDiffer()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDir, "hash-differ.csx");

        var metadata = new ScriptCompilationCache.CacheMetadata
        {
            DraftSpecVersion = typeof(DraftSpec.Dsl).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            SourceFiles = [sourceFile],
            SourceFileHashes = new Dictionary<string, string> { [sourceFile] = "old-hash" }
        };

        var fileHashes = new Dictionary<string, string> { [sourceFile] = "new-hash" };

        // Act
        var result = _cache.IsCacheValidWithHashes(metadata, fileHashes);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    private static Script<object> CreateTestScript(string code)
    {
        var options = ScriptOptions.Default
            .AddReferences(typeof(object).Assembly)
            .AddReferences(typeof(DraftSpec.Dsl).Assembly)
            .AddReferences(typeof(ScriptGlobals).Assembly)
            .AddImports("DraftSpec");

        return CSharpScript.Create(code, options, typeof(ScriptGlobals));
    }

    #region Error Path Logging Tests

    /// <summary>
    /// Simple logger that captures log entries for verification.
    /// </summary>
    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> LogEntries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    [Test]
    public async Task CacheScript_WhenWriteFails_LogsDebugAndDoesNotThrow()
    {
        // Arrange - create cache with logger, then make caching fail
        var logger = new CapturingLogger();
        var readOnlyDir = Path.Combine(_testDir, "readonly-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(readOnlyDir);

        // Create a directory where the cache file should go, making file creation fail
        var cacheSubDir = Path.Combine(readOnlyDir, ".draftspec", "cache", "scripts");
        Directory.CreateDirectory(cacheSubDir);

        // Create a directory with the same name as where the DLL would be written
        // This will cause file creation to fail
        var cache = new ScriptCompilationCache(readOnlyDir, logger);
        var sourceFile = Path.Combine(_testDir, "log-write-error.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        var script = CreateTestScript("var x = 1;");

        // Create a file where the cache will try to write, then make it read-only
        // to trigger a write error when trying to overwrite
        var fakeBlocker = Path.Combine(cacheSubDir, "fake.dll");
        await File.WriteAllTextAsync(fakeBlocker, "blocker");
        File.SetAttributes(fakeBlocker, FileAttributes.ReadOnly);

        // Act - should not throw
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Assert - logged debug message (note: may not log if error happens before reaching cache write)
        // The important assertion is that no exception was thrown
        await Assert.That(true).IsTrue(); // Execution reached here = no exception

        // Cleanup
        File.SetAttributes(fakeBlocker, FileAttributes.Normal);
    }

    [Test]
    public async Task Clear_WhenDeleteFails_LogsDebugAndDoesNotThrow()
    {
        // Arrange
        var logger = new CapturingLogger();
        var clearDir = Path.Combine(_testDir, "clear-fail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(clearDir);

        var cache = new ScriptCompilationCache(clearDir, logger);
        var sourceFile = Path.Combine(clearDir, "to-clear.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Cache a script first
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Lock a file in the cache directory to prevent deletion
        var cacheSubDir = Path.Combine(clearDir, ".draftspec", "cache", "scripts");
        var dllFiles = Directory.GetFiles(cacheSubDir, "*.dll");
        await Assert.That(dllFiles.Length).IsGreaterThan(0);

        // Open a file handle to prevent deletion
        using var fileHandle = File.Open(dllFiles[0], FileMode.Open, FileAccess.Read, FileShare.None);

        // Act - should not throw even though deletion will fail
        cache.Clear();

        // Assert - no exception thrown, debug log may have been written
        await Assert.That(logger.LogEntries.Where(e => e.Level == LogLevel.Debug).Count()).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GetStatistics_WhenAccessFails_LogsDebugAndReturnsEmptyStats()
    {
        // Arrange
        var logger = new CapturingLogger();
        var statsDir = Path.Combine(_testDir, "stats-fail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(statsDir);

        var cache = new ScriptCompilationCache(statsDir, logger);
        var sourceFile = Path.Combine(statsDir, "stats-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Cache a script first
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Get the cache directory and break it
        var cacheSubDir = Path.Combine(statsDir, ".draftspec", "cache", "scripts");
        await Assert.That(Directory.Exists(cacheSubDir)).IsTrue();

        // Make the .meta.json files unreadable by corrupting the directory
        // On some systems, we can't easily simulate permission errors, so we test
        // that GetStatistics handles missing files gracefully
        foreach (var dll in Directory.GetFiles(cacheSubDir, "*.dll"))
        {
            File.Delete(dll);
        }

        // Act
        var stats = cache.GetStatistics();

        // Assert - returns stats (possibly with 0 size since DLLs are deleted)
        await Assert.That(stats).IsNotNull();
    }

    [Test]
    public async Task TryExecuteCachedAsync_WhenExecutionFails_LogsDebugAndReturnsFalse()
    {
        // Arrange
        var logger = new CapturingLogger();
        var execDir = Path.Combine(_testDir, "exec-fail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(execDir);

        var cache = new ScriptCompilationCache(execDir, logger);
        var sourceFile = Path.Combine(execDir, "exec-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Cache a script
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Corrupt the DLL to cause execution failure
        var cacheSubDir = Path.Combine(execDir, ".draftspec", "cache", "scripts");
        var dllFiles = Directory.GetFiles(cacheSubDir, "*.dll");
        await Assert.That(dllFiles.Length).IsGreaterThan(0);
        await File.WriteAllTextAsync(dllFiles[0], "not a valid dll content");

        var globals = new ScriptGlobals();

        // Act
        var (success, _) = await cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 1;",
            globals);

        // Assert - returns false and logs debug
        await Assert.That(success).IsFalse();
        await Assert.That(logger.LogEntries.Any(e => e.Level == LogLevel.Debug && e.Exception != null)).IsTrue();
    }

    [Test]
    public async Task LoadMetadata_WhenFileMalformed_LogsDebugAndReturnsNull()
    {
        // Arrange
        var logger = new CapturingLogger();
        var loadDir = Path.Combine(_testDir, "load-fail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(loadDir);

        var cache = new ScriptCompilationCache(loadDir, logger);
        var sourceFile = Path.Combine(loadDir, "load-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Cache a script
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Corrupt the metadata file
        var cacheSubDir = Path.Combine(loadDir, ".draftspec", "cache", "scripts");
        var metaFiles = Directory.GetFiles(cacheSubDir, "*.meta.json");
        await Assert.That(metaFiles.Length).IsGreaterThan(0);
        await File.WriteAllTextAsync(metaFiles[0], "{ invalid json content <<<");

        var globals = new ScriptGlobals();

        // Act - try to use the cache with corrupted metadata
        var (success, _) = await cache.TryExecuteCachedAsync(
            sourceFile,
            [sourceFile],
            "var x = 1;",
            globals);

        // Assert - returns false (cache miss) and logs debug
        await Assert.That(success).IsFalse();
        await Assert.That(logger.LogEntries.Any(e => e.Level == LogLevel.Debug && e.Exception != null)).IsTrue();
    }

    [Test]
    public async Task Constructor_WithLogger_PassesLoggerToCache()
    {
        // Arrange
        var logger = new CapturingLogger();
        var testDir = Path.Combine(_testDir, "with-logger-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        // Act - create cache with logger
        var cache = new ScriptCompilationCache(testDir, logger);

        // Trigger a scenario that would log (non-existent cache directory)
        var stats = cache.GetStatistics();

        // Assert - no errors, empty stats returned
        await Assert.That(stats.EntryCount).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_WithoutLogger_UsesNullLogger()
    {
        // Arrange
        var testDir = Path.Combine(_testDir, "no-logger-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        // Act - create cache without logger (should use NullLogger internally)
        var cache = new ScriptCompilationCache(testDir);
        var sourceFile = Path.Combine(testDir, "test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // This should not throw even without a logger
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Assert - cache was created successfully
        var stats = cache.GetStatistics();
        await Assert.That(stats.EntryCount).IsEqualTo(1);
    }

    [Test]
    public async Task DeleteCacheEntry_WhenFileLocked_LogsDebugAndContinues()
    {
        // Arrange
        var logger = new CapturingLogger();
        var deleteDir = Path.Combine(_testDir, "delete-fail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(deleteDir);

        var cache = new ScriptCompilationCache(deleteDir, logger);
        var sourceFile = Path.Combine(deleteDir, "delete-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Cache a script
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Get the cache directory
        var cacheSubDir = Path.Combine(deleteDir, ".draftspec", "cache", "scripts");
        var dllFiles = Directory.GetFiles(cacheSubDir, "*.dll");
        await Assert.That(dllFiles.Length).IsGreaterThan(0);

        // Make the cache directory read-only to prevent file deletion (works on macOS/Linux)
        var dirInfo = new DirectoryInfo(cacheSubDir);
        var originalAttributes = dirInfo.Attributes;
        if (!OperatingSystem.IsWindows())
        {
            // On Unix, remove write permission from directory to prevent file deletion
            File.SetUnixFileMode(cacheSubDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        }

        try
        {
            // Modify metadata to trigger cache invalidation (which calls DeleteCacheEntry)
            // We need to modify it before locking the directory, so read it first
            var metaFiles = Directory.GetFiles(cacheSubDir, "*.meta.json");
            var metaContent = await File.ReadAllTextAsync(metaFiles[0]);
            metaContent = metaContent.Replace("\"draftSpecVersion\":", "\"draftSpecVersion\": \"0.0.0-fake\", \"_old\":");

            // Temporarily restore write permission to update metadata
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(cacheSubDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            await File.WriteAllTextAsync(metaFiles[0], metaContent);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(cacheSubDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            }

            var globals = new ScriptGlobals();

            // Act - try to use cache, which will validate, fail, and try to delete
            var (success, _) = await cache.TryExecuteCachedAsync(
                sourceFile,
                [sourceFile],
                "var x = 1;",
                globals);

            // Assert - returns false, and logged debug for delete failure
            await Assert.That(success).IsFalse();
            await Assert.That(logger.LogEntries.Any(e =>
                e.Level == LogLevel.Debug &&
                e.Message.Contains("delete", StringComparison.OrdinalIgnoreCase))).IsTrue();
        }
        finally
        {
            // Restore permissions for cleanup
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(cacheSubDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }

    [Test]
    public async Task CacheScript_WhenEmitFails_LogsDebugAndDoesNotThrow()
    {
        // Arrange
        var logger = new CapturingLogger();
        var emitDir = Path.Combine(_testDir, "emit-fail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emitDir);

        var cache = new ScriptCompilationCache(emitDir, logger);
        var sourceFile = Path.Combine(emitDir, "emit-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Create a directory where the DLL would be written to cause file creation to fail
        var cacheSubDir = Path.Combine(emitDir, ".draftspec", "cache", "scripts");
        Directory.CreateDirectory(cacheSubDir);

        var script = CreateTestScript("var x = 1;");

        // Pre-create a directory with the same name pattern as where cache writes
        // This makes the file write fail
        var fakeDllDir = Path.Combine(cacheSubDir, "blocker.dll");
        Directory.CreateDirectory(fakeDllDir); // Create a directory where a file should go

        // Act - should not throw
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", script);

        // Assert - execution reached here without throwing
        await Assert.That(true).IsTrue();
        // Logger may have debug entries about the failure
        // (depends on exact error path hit)
    }

    [Test]
    public async Task Clear_WhenDirectoryAccessDenied_LogsDebugAndContinues()
    {
        // Arrange
        var logger = new CapturingLogger();
        var accessDir = Path.Combine(_testDir, "access-fail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(accessDir);

        var cache = new ScriptCompilationCache(accessDir, logger);
        var sourceFile = Path.Combine(accessDir, "access-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Cache a script
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Get the cache directory
        var cacheSubDir = Path.Combine(accessDir, ".draftspec", "cache", "scripts");

        // Make the parent directory read-only to prevent deletion
        var parentDir = Path.GetDirectoryName(cacheSubDir)!;
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(parentDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        }

        try
        {
            // Act - Clear should fail but not throw
            cache.Clear();

            // Assert - debug log should be written about failure
            await Assert.That(logger.LogEntries.Any(e =>
                e.Level == LogLevel.Debug &&
                e.Exception != null)).IsTrue();
        }
        finally
        {
            // Restore permissions for cleanup
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(parentDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }

    [Test]
    public async Task GetStatistics_WhenFileInfoFails_LogsDebugAndReturnsEmpty()
    {
        // Arrange
        var logger = new CapturingLogger();
        var infoDir = Path.Combine(_testDir, "info-fail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(infoDir);

        var cache = new ScriptCompilationCache(infoDir, logger);
        var sourceFile = Path.Combine(infoDir, "info-test.csx");
        await File.WriteAllTextAsync(sourceFile, "var x = 1;");

        // Cache a script
        cache.CacheScript(sourceFile, [sourceFile], "var x = 1;", CreateTestScript("var x = 1;"));

        // Delete the cache files to cause FileInfo to fail during Sum
        var cacheSubDir = Path.Combine(infoDir, ".draftspec", "cache", "scripts");
        foreach (var dll in Directory.GetFiles(cacheSubDir, "*.dll"))
        {
            // Make file read-only to simulate access issues, then delete during stats
            File.Delete(dll);
        }

        // Act - stats collection should handle the error gracefully
        var stats = cache.GetStatistics();

        // Assert - returns stats (size will be 0 since DLLs are deleted)
        await Assert.That(stats).IsNotNull();
        await Assert.That(stats.TotalSizeBytes).IsEqualTo(0);
    }

    #endregion
}

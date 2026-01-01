using DraftSpec.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

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

    private static Script<object> CreateTestScript(string code)
    {
        var options = ScriptOptions.Default
            .AddReferences(typeof(object).Assembly)
            .AddReferences(typeof(DraftSpec.Dsl).Assembly)
            .AddReferences(typeof(ScriptGlobals).Assembly)
            .AddImports("DraftSpec");

        return CSharpScript.Create(code, options, typeof(ScriptGlobals));
    }
}

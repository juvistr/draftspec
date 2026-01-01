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

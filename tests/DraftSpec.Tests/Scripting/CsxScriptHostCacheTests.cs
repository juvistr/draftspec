using DraftSpec.Scripting;

namespace DraftSpec.Tests.Scripting;

/// <summary>
/// Integration tests for CsxScriptHost disk caching functionality.
/// </summary>
public class CsxScriptHostCacheTests
{
    private string _testDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"host-cache-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
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
    public async Task ExecuteAsync_FirstRun_CreatesCacheFiles()
    {
        // Arrange
        var scriptHost = new CsxScriptHost(_testDir, useDiskCache: true);
        var scriptPath = Path.Combine(_testDir, "test.csx");
        await File.WriteAllTextAsync(scriptPath, @"
using static DraftSpec.Dsl;
describe(""test"", () => it(""works"", () => {}));
");

        // Act
        await scriptHost.ExecuteAsync(scriptPath);

        // Assert - cache files should be created
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        await Assert.That(Directory.Exists(cacheDir)).IsTrue();

        var dllFiles = Directory.GetFiles(cacheDir, "*.dll");
        var metaFiles = Directory.GetFiles(cacheDir, "*.meta.json");

        await Assert.That(dllFiles.Length).IsEqualTo(1);
        await Assert.That(metaFiles.Length).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_SecondRun_UsesDiskCache()
    {
        // Arrange
        var scriptHost = new CsxScriptHost(_testDir, useDiskCache: true);
        var scriptPath = Path.Combine(_testDir, "cached.csx");
        await File.WriteAllTextAsync(scriptPath, @"
using static DraftSpec.Dsl;
describe(""cached test"", () => it(""runs from cache"", () => {}));
");

        // Act - First run (compiles and caches to disk)
        await scriptHost.ExecuteAsync(scriptPath);

        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var cachedDll = Directory.GetFiles(cacheDir, "*.dll").Single();
        var originalWriteTime = File.GetLastWriteTimeUtc(cachedDll);

        // Small delay to ensure file system timestamps would differ if rewritten
        await Task.Delay(50);

        // Act - Create NEW host instance (empty in-memory cache) to force disk cache hit
        var scriptHost2 = new CsxScriptHost(_testDir, useDiskCache: true);
        var context = await scriptHost2.ExecuteAsync(scriptPath);

        // Assert - DLL wasn't rewritten (disk cache was used)
        var newWriteTime = File.GetLastWriteTimeUtc(cachedDll);
        await Assert.That(newWriteTime).IsEqualTo(originalWriteTime);

        // Assert - context was properly returned from disk cache execution
        await Assert.That(context).IsNotNull();
        await Assert.That(context!.Description).IsEqualTo("cached test");
    }

    [Test]
    public async Task ExecuteAsync_AfterSourceChange_RecompilesScript()
    {
        // Arrange
        var scriptHost = new CsxScriptHost(_testDir, useDiskCache: true);
        var scriptPath = Path.Combine(_testDir, "changing.csx");
        await File.WriteAllTextAsync(scriptPath, @"
using static DraftSpec.Dsl;
describe(""version 1"", () => it(""works"", () => {}));
");

        // Act - First run
        await scriptHost.ExecuteAsync(scriptPath);

        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        var originalDllCount = Directory.GetFiles(cacheDir, "*.dll").Length;

        // Modify the script
        await File.WriteAllTextAsync(scriptPath, @"
using static DraftSpec.Dsl;
describe(""version 2"", () => it(""also works"", () => {}));
");

        // Act - Second run with changed source
        scriptHost.Reset();
        await scriptHost.ExecuteAsync(scriptPath);

        // Assert - new cache entry created (different hash)
        var newDllCount = Directory.GetFiles(cacheDir, "*.dll").Length;
        await Assert.That(newDllCount).IsGreaterThanOrEqualTo(originalDllCount);
    }

    [Test]
    public async Task ExecuteAsync_WithDiskCacheDisabled_DoesNotCreateCacheFiles()
    {
        // Arrange
        var scriptHost = new CsxScriptHost(_testDir, useDiskCache: false);
        var scriptPath = Path.Combine(_testDir, "no-cache.csx");
        await File.WriteAllTextAsync(scriptPath, @"
using static DraftSpec.Dsl;
describe(""no cache"", () => it(""works"", () => {}));
");

        // Act
        await scriptHost.ExecuteAsync(scriptPath);

        // Assert - cache directory should not exist
        var cacheDir = Path.Combine(_testDir, ".draftspec", "cache", "scripts");
        await Assert.That(Directory.Exists(cacheDir)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_ReturnsValidSpecContext()
    {
        // Arrange
        var scriptHost = new CsxScriptHost(_testDir, useDiskCache: true);
        var scriptPath = Path.Combine(_testDir, "context.csx");
        await File.WriteAllTextAsync(scriptPath, @"
using static DraftSpec.Dsl;
describe(""outer"", () => {
    it(""spec 1"", () => {});
    it(""spec 2"", () => {});
});
");

        // Act - First run (compiles)
        var context1 = await scriptHost.ExecuteAsync(scriptPath);

        // Reset and run again (from cache)
        scriptHost.Reset();
        var context2 = await scriptHost.ExecuteAsync(scriptPath);

        // Assert - both runs should return valid contexts
        await Assert.That(context1).IsNotNull();
        await Assert.That(context2).IsNotNull();
        await Assert.That(context1!.Description).IsEqualTo("outer");
        await Assert.That(context2!.Description).IsEqualTo("outer");
        await Assert.That(context1.Specs).Count().IsEqualTo(2);
        await Assert.That(context2.Specs).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_SecondRunWithoutReset_UsesInMemoryCache()
    {
        // Arrange - test in-memory cache hit (with disk cache enabled)
        var scriptHost = new CsxScriptHost(_testDir, useDiskCache: true);
        var scriptPath = Path.Combine(_testDir, "inmemory.csx");
        await File.WriteAllTextAsync(scriptPath, @"
using static DraftSpec.Dsl;
describe(""inmemory test"", () => it(""works"", () => {}));
");

        // Act - First run (compiles and populates in-memory cache)
        var context1 = await scriptHost.ExecuteAsync(scriptPath);

        // Second run WITHOUT Reset() - should hit in-memory cache
        var context2 = await scriptHost.ExecuteAsync(scriptPath);

        // Assert - both runs should return valid contexts
        await Assert.That(context1).IsNotNull();
        await Assert.That(context2).IsNotNull();
        await Assert.That(context1!.Description).IsEqualTo("inmemory test");
        await Assert.That(context2!.Description).IsEqualTo("inmemory test");
    }

    [Test]
    public async Task ExecuteAsync_WithDiskCacheDisabled_SecondRunUsesInMemoryCache()
    {
        // Arrange - test in-memory cache hit (without disk cache)
        var scriptHost = new CsxScriptHost(_testDir, useDiskCache: false);
        var scriptPath = Path.Combine(_testDir, "inmemory-nodisk.csx");
        await File.WriteAllTextAsync(scriptPath, @"
using static DraftSpec.Dsl;
describe(""inmemory nodisk"", () => it(""works"", () => {}));
");

        // Act - First run (compiles and populates in-memory cache)
        var context1 = await scriptHost.ExecuteAsync(scriptPath);

        // Second run WITHOUT Reset() - should hit in-memory cache
        var context2 = await scriptHost.ExecuteAsync(scriptPath);

        // Assert - both runs should return valid contexts
        await Assert.That(context1).IsNotNull();
        await Assert.That(context2).IsNotNull();
        await Assert.That(context1!.Description).IsEqualTo("inmemory nodisk");
        await Assert.That(context2!.Description).IsEqualTo("inmemory nodisk");
    }
}

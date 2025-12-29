using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for InMemoryBuildCache.
/// </summary>
public class BuildCacheTests
{
    #region NeedsRebuild Tests

    [Test]
    public async Task NeedsRebuild_NeverBuilt_ReturnsTrue()
    {
        var cache = new InMemoryBuildCache();

        var result = cache.NeedsRebuild("/some/directory", DateTime.UtcNow);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NeedsRebuild_SourceNotModified_ReturnsFalse()
    {
        var cache = new InMemoryBuildCache();
        var buildTime = DateTime.UtcNow;
        var sourceTime = buildTime.AddMinutes(-5);

        cache.UpdateCache("/some/directory", buildTime, sourceTime);

        // Same source time - no changes
        var result = cache.NeedsRebuild("/some/directory", sourceTime);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task NeedsRebuild_SourceModifiedAfterBuild_ReturnsTrue()
    {
        var cache = new InMemoryBuildCache();
        var buildTime = DateTime.UtcNow;
        var originalSourceTime = buildTime.AddMinutes(-5);

        cache.UpdateCache("/some/directory", buildTime, originalSourceTime);

        // Source modified after build
        var newerSourceTime = buildTime.AddMinutes(5);
        var result = cache.NeedsRebuild("/some/directory", newerSourceTime);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NeedsRebuild_DifferentDirectory_ReturnsTrue()
    {
        var cache = new InMemoryBuildCache();
        var buildTime = DateTime.UtcNow;
        var sourceTime = buildTime.AddMinutes(-5);

        cache.UpdateCache("/directory/a", buildTime, sourceTime);

        // Different directory - not cached
        var result = cache.NeedsRebuild("/directory/b", sourceTime);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NeedsRebuild_SourceOlderThanCached_ReturnsFalse()
    {
        var cache = new InMemoryBuildCache();
        var buildTime = DateTime.UtcNow;
        var cachedSourceTime = buildTime.AddMinutes(-5);

        cache.UpdateCache("/some/directory", buildTime, cachedSourceTime);

        // Source is older than what we cached - no need to rebuild
        var olderSourceTime = cachedSourceTime.AddMinutes(-10);
        var result = cache.NeedsRebuild("/some/directory", olderSourceTime);

        await Assert.That(result).IsFalse();
    }

    #endregion

    #region UpdateCache Tests

    [Test]
    public async Task UpdateCache_StoresValues_ForLaterRetrieval()
    {
        var cache = new InMemoryBuildCache();
        var buildTime = DateTime.UtcNow;
        var sourceTime = buildTime.AddMinutes(-1);

        cache.UpdateCache("/test/dir", buildTime, sourceTime);

        // Should not need rebuild for same source time
        await Assert.That(cache.NeedsRebuild("/test/dir", sourceTime)).IsFalse();
    }

    [Test]
    public async Task UpdateCache_OverwritesPreviousValues()
    {
        var cache = new InMemoryBuildCache();
        var oldBuildTime = DateTime.UtcNow.AddHours(-1);
        var oldSourceTime = oldBuildTime.AddMinutes(-5);

        cache.UpdateCache("/test/dir", oldBuildTime, oldSourceTime);

        var newBuildTime = DateTime.UtcNow;
        var newSourceTime = newBuildTime.AddMinutes(-2);

        cache.UpdateCache("/test/dir", newBuildTime, newSourceTime);

        // Should use the new cached source time
        await Assert.That(cache.NeedsRebuild("/test/dir", newSourceTime)).IsFalse();

        // Source modified after new cached time - needs rebuild
        await Assert.That(cache.NeedsRebuild("/test/dir", newSourceTime.AddSeconds(1))).IsTrue();
    }

    #endregion

    #region Clear Tests

    [Test]
    public async Task Clear_RemovesAllCachedData()
    {
        var cache = new InMemoryBuildCache();
        var buildTime = DateTime.UtcNow;
        var sourceTime = buildTime.AddMinutes(-5);

        cache.UpdateCache("/dir/a", buildTime, sourceTime);
        cache.UpdateCache("/dir/b", buildTime, sourceTime);

        cache.Clear();

        // Both directories should need rebuild after clear
        await Assert.That(cache.NeedsRebuild("/dir/a", sourceTime)).IsTrue();
        await Assert.That(cache.NeedsRebuild("/dir/b", sourceTime)).IsTrue();
    }

    [Test]
    public async Task Clear_EmptyCache_DoesNotThrow()
    {
        var cache = new InMemoryBuildCache();

        // Should not throw
        cache.Clear();

        await Task.CompletedTask;
    }

    #endregion

    #region Multiple Directories Tests

    [Test]
    public async Task MultipleDirectories_TrackedIndependently()
    {
        var cache = new InMemoryBuildCache();
        var now = DateTime.UtcNow;

        var sourceTimeA = now.AddMinutes(-10);
        var sourceTimeB = now.AddMinutes(-5);

        cache.UpdateCache("/dir/a", now, sourceTimeA);
        cache.UpdateCache("/dir/b", now, sourceTimeB);

        // Both directories should not need rebuild for their cached times
        await Assert.That(cache.NeedsRebuild("/dir/a", sourceTimeA)).IsFalse();
        await Assert.That(cache.NeedsRebuild("/dir/b", sourceTimeB)).IsFalse();

        // But should need rebuild if source is newer
        await Assert.That(cache.NeedsRebuild("/dir/a", now)).IsTrue();
        await Assert.That(cache.NeedsRebuild("/dir/b", now)).IsTrue();
    }

    #endregion
}

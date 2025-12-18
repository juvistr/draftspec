using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for LruCache.
/// </summary>
public class LruCacheTests
{
    #region Basic Operations

    [Test]
    public async Task Set_AndGet_ReturnsValue()
    {
        var cache = new LruCache<string, int>(10);

        cache.Set("key1", 42);

        await Assert.That(cache.TryGetValue("key1", out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task TryGetValue_NonExistentKey_ReturnsFalse()
    {
        var cache = new LruCache<string, int>(10);

        await Assert.That(cache.TryGetValue("nonexistent", out _)).IsFalse();
    }

    [Test]
    public async Task Count_TracksItems()
    {
        var cache = new LruCache<string, int>(10);

        await Assert.That(cache.Count).IsEqualTo(0);

        cache.Set("key1", 1);
        await Assert.That(cache.Count).IsEqualTo(1);

        cache.Set("key2", 2);
        await Assert.That(cache.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Capacity_ReturnsConfiguredCapacity()
    {
        var cache = new LruCache<string, int>(42);

        await Assert.That(cache.Capacity).IsEqualTo(42);
    }

    [Test]
    public async Task Clear_RemovesAllItems()
    {
        var cache = new LruCache<string, int>(10);
        cache.Set("key1", 1);
        cache.Set("key2", 2);

        cache.Clear();

        await Assert.That(cache.Count).IsEqualTo(0);
        await Assert.That(cache.TryGetValue("key1", out _)).IsFalse();
    }

    [Test]
    public async Task ContainsKey_ReturnsCorrectResult()
    {
        var cache = new LruCache<string, int>(10);
        cache.Set("key1", 1);

        await Assert.That(cache.ContainsKey("key1")).IsTrue();
        await Assert.That(cache.ContainsKey("key2")).IsFalse();
    }

    #endregion

    #region LRU Eviction

    [Test]
    public async Task Set_AtCapacity_EvictsLeastRecentlyUsed()
    {
        var cache = new LruCache<string, int>(3);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        // Cache is full, adding new item should evict "a" (oldest)
        cache.Set("d", 4);

        await Assert.That(cache.Count).IsEqualTo(3);
        await Assert.That(cache.ContainsKey("a")).IsFalse(); // Evicted
        await Assert.That(cache.ContainsKey("b")).IsTrue();
        await Assert.That(cache.ContainsKey("c")).IsTrue();
        await Assert.That(cache.ContainsKey("d")).IsTrue();
    }

    [Test]
    public async Task TryGetValue_UpdatesAccessOrder()
    {
        var cache = new LruCache<string, int>(3);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        // Access "a" to make it most recently used
        cache.TryGetValue("a", out _);

        // Add new item - should evict "b" (now the oldest)
        cache.Set("d", 4);

        await Assert.That(cache.ContainsKey("a")).IsTrue(); // Accessed, not evicted
        await Assert.That(cache.ContainsKey("b")).IsFalse(); // Evicted
        await Assert.That(cache.ContainsKey("c")).IsTrue();
        await Assert.That(cache.ContainsKey("d")).IsTrue();
    }

    [Test]
    public async Task Set_ExistingKey_UpdatesValue_AndAccessOrder()
    {
        var cache = new LruCache<string, int>(3);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        // Update "a" with new value
        cache.Set("a", 100);

        // Add new item - should evict "b" (oldest non-updated)
        cache.Set("d", 4);

        await Assert.That(cache.TryGetValue("a", out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(100);
        await Assert.That(cache.ContainsKey("b")).IsFalse(); // Evicted
    }

    [Test]
    public async Task GetOrAdd_ExistingKey_ReturnsExisting_DoesNotCallFactory()
    {
        var cache = new LruCache<string, int>(10);
        cache.Set("key", 42);
        var factoryCalled = false;

        var result = cache.GetOrAdd("key", _ =>
        {
            factoryCalled = true;
            return 999;
        });

        await Assert.That(result).IsEqualTo(42);
        await Assert.That(factoryCalled).IsFalse();
    }

    [Test]
    public async Task GetOrAdd_NewKey_CallsFactory_CachesResult()
    {
        var cache = new LruCache<string, int>(10);
        var factoryCallCount = 0;

        var result1 = cache.GetOrAdd("key", _ =>
        {
            factoryCallCount++;
            return 42;
        });

        var result2 = cache.GetOrAdd("key", _ =>
        {
            factoryCallCount++;
            return 999;
        });

        await Assert.That(result1).IsEqualTo(42);
        await Assert.That(result2).IsEqualTo(42);
        await Assert.That(factoryCallCount).IsEqualTo(1); // Factory only called once
    }

    [Test]
    public async Task GetOrAdd_AtCapacity_EvictsLeastRecentlyUsed()
    {
        var cache = new LruCache<string, int>(3);

        cache.GetOrAdd("a", _ => 1);
        cache.GetOrAdd("b", _ => 2);
        cache.GetOrAdd("c", _ => 3);

        // Access "a" to make it most recently used
        cache.GetOrAdd("a", _ => 999);

        // Add new item - should evict "b" (oldest non-accessed)
        cache.GetOrAdd("d", _ => 4);

        await Assert.That(cache.ContainsKey("a")).IsTrue();
        await Assert.That(cache.ContainsKey("b")).IsFalse(); // Evicted
        await Assert.That(cache.ContainsKey("c")).IsTrue();
        await Assert.That(cache.ContainsKey("d")).IsTrue();
    }

    #endregion

    #region Thread Safety

    [Test]
    public async Task ConcurrentAccess_IsThreadSafe()
    {
        var cache = new LruCache<int, int>(100);
        var tasks = new List<Task>();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Spawn multiple threads performing various operations
        for (var i = 0; i < 100; i++)
        {
            var key = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    cache.Set(key, key * 10);
                    cache.TryGetValue(key, out _);
                    cache.GetOrAdd(key + 1000, k => k * 10);
                    _ = cache.ContainsKey(key);
                    _ = cache.Count;
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ConcurrentEviction_IsThreadSafe()
    {
        var cache = new LruCache<int, int>(10);
        var tasks = new List<Task>();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Many concurrent writes to force evictions
        for (var i = 0; i < 1000; i++)
        {
            var key = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    cache.Set(key, key);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        await Assert.That(errors).IsEmpty();
        await Assert.That(cache.Count).IsLessThanOrEqualTo(10);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Constructor_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, int>(0));
    }

    [Test]
    public void Constructor_NegativeCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, int>(-1));
    }

    [Test]
    public async Task Capacity_One_WorksCorrectly()
    {
        var cache = new LruCache<string, int>(1);

        cache.Set("a", 1);
        await Assert.That(cache.TryGetValue("a", out var v1)).IsTrue();
        await Assert.That(v1).IsEqualTo(1);

        cache.Set("b", 2);
        await Assert.That(cache.ContainsKey("a")).IsFalse();
        await Assert.That(cache.TryGetValue("b", out var v2)).IsTrue();
        await Assert.That(v2).IsEqualTo(2);
    }

    [Test]
    public async Task NullValue_IsSupported()
    {
        var cache = new LruCache<string, string?>(10);

        cache.Set("key", null);

        await Assert.That(cache.TryGetValue("key", out var value)).IsTrue();
        await Assert.That(value).IsNull();
    }

    #endregion
}

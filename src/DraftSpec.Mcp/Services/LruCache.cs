namespace DraftSpec.Mcp.Services;

/// <summary>
/// Thread-safe LRU (Least Recently Used) cache with configurable capacity.
/// Evicts least recently used items when capacity is exceeded.
/// </summary>
/// <typeparam name="TKey">The type of cache keys</typeparam>
/// <typeparam name="TValue">The type of cached values</typeparam>
public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, LinkedListNode<CacheEntry>> _cache;
    private readonly LinkedList<CacheEntry> _lruList;
    private readonly object _lock = new();
    private readonly int _capacity;

    private record CacheEntry(TKey Key, TValue Value);

    /// <summary>
    /// Creates a new LRU cache with the specified capacity.
    /// </summary>
    /// <param name="capacity">Maximum number of items to cache</param>
    public LruCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheEntry>>(capacity);
        _lruList = new LinkedList<CacheEntry>();
    }

    /// <summary>
    /// Gets the current number of items in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// Gets the maximum capacity of the cache.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Tries to get a value from the cache.
    /// If found, the item is moved to the front (most recently used).
    /// </summary>
    /// <param name="key">The key to look up</param>
    /// <param name="value">The cached value if found</param>
    /// <returns>True if the key was found, false otherwise</returns>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                value = node.Value.Value;
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// Adds or updates a value in the cache.
    /// If the cache is at capacity, the least recently used item is evicted.
    /// </summary>
    /// <param name="key">The key to add or update</param>
    /// <param name="value">The value to cache</param>
    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                // Update existing entry and move to front
                _lruList.Remove(existingNode);
                var newEntry = new CacheEntry(key, value);
                var newNode = _lruList.AddFirst(newEntry);
                _cache[key] = newNode;
                return;
            }

            // Evict if at capacity
            while (_cache.Count >= _capacity && _lruList.Last != null)
            {
                var lastNode = _lruList.Last;
                _cache.Remove(lastNode.Value.Key);
                _lruList.RemoveLast();
            }

            // Add new entry at front
            var entry = new CacheEntry(key, value);
            var node = _lruList.AddFirst(entry);
            _cache[key] = node;
        }
    }

    /// <summary>
    /// Gets or adds a value using a factory function.
    /// Thread-safe: only one thread will call the factory for a given key.
    /// </summary>
    /// <param name="key">The key to look up or add</param>
    /// <param name="valueFactory">Factory function to create the value if not cached</param>
    /// <returns>The cached or newly created value</returns>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                // Move to front (most recently used)
                _lruList.Remove(existingNode);
                _lruList.AddFirst(existingNode);
                return existingNode.Value.Value;
            }

            // Create new value
            var value = valueFactory(key);

            // Evict if at capacity
            while (_cache.Count >= _capacity && _lruList.Last != null)
            {
                var lastNode = _lruList.Last;
                _cache.Remove(lastNode.Value.Key);
                _lruList.RemoveLast();
            }

            // Add new entry at front
            var entry = new CacheEntry(key, value);
            var node = _lruList.AddFirst(entry);
            _cache[key] = node;

            return value;
        }
    }

    /// <summary>
    /// Removes all items from the cache.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
        }
    }

    /// <summary>
    /// Checks if the cache contains the specified key.
    /// Does NOT update the access order.
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key exists in the cache</returns>
    public bool ContainsKey(TKey key)
    {
        lock (_lock)
        {
            return _cache.ContainsKey(key);
        }
    }
}

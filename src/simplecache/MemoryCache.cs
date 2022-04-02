using System.Collections.Concurrent;

namespace simplecache;

public class MemoryCache<T>

{
    private readonly ConcurrentDictionary<object, T> _dictionary;
    private ConcurrentQueue<object> _keyQueue { get; set; } = new();
    private CacheOptions _options { get; init; } = new();

    public MemoryCache()
    {
        _dictionary = new();
    }

    public MemoryCache(CacheOptions options)
        : this()
    {
        _options = options;
    }

    public int Count => _dictionary.Count;
    
    public bool Set(object key, T value)
    {
        var success =_dictionary.TryAdd(key, value);
        if (success)
            _keyQueue.Enqueue(key);
        
        if (_dictionary.Count > _options.SizeLimit)
        {
            var pruned = TryPrune();
            if (!pruned)
                return false;
        }
        
        return success;
    }
    
    public T? GetOrSet(object key, Func<object,T> dataFetcher)
    {
        var success = _dictionary.TryGetValue(key, out var value);
        if (!success)
        {
            value = dataFetcher(key);
            success = Set(key, value);
        }
        return success ? value : default;
    }

    public bool TryGet(object key, out T value)
    {
        var success = _dictionary.TryGetValue(key, out value!);
        if (success && _options.EvictionPolicy == Evict.LeastRecentlyUsed)
            Refresh(key);
        
        return success;
    }

    private void Refresh(object key)
    {
        _keyQueue = new ConcurrentQueue<object>(_keyQueue.Where(x => !x.Equals(key)));
        _keyQueue.Enqueue(key);
    }

    public bool TryPrune()
    {
        _keyQueue.TryDequeue(out var key);
        return _dictionary.TryRemove(key!, out _);
    }
}
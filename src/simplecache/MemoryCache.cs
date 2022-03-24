using System.Collections.Concurrent;

namespace simplecache;

public class MemoryCache<T>

{
    private readonly ConcurrentDictionary<object, T> _dictionary;
    private readonly ConcurrentQueue<object> _keyQueue;
    private CacheOptions _options { get; init; } = new();

    public MemoryCache()
    {
        _dictionary = new();
        _keyQueue = new();
    }

    public MemoryCache(CacheOptions options)
        : this()
    {
        _options = options;
    }
    
    public async Task<bool> Add(object key, T value)
    {
        var success =_dictionary.TryAdd(key, value);
        return success;
    }

    public async Task<(bool, T)> TryGet(object key)
    {
        var success = _dictionary.TryGetValue(key, out var value);
        
        return (success, value!);
    }
}
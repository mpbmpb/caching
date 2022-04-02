using System.Collections.Concurrent;

namespace simplecache;

public class ThreadSafeCache<T>
{
    private readonly ConcurrentDictionary<object, T> _dictionary;
    private ConcurrentQueue<object> _keyQueue { get; set; } = new();
    private CacheOptions _options { get; init; } = new();
    private readonly ReaderWriterLockSlim _cacheLock = new();
    private readonly ReaderWriterLockSlim _queueLock = new();

    public ThreadSafeCache()
    {
        _dictionary = new();
    }

    public ThreadSafeCache(CacheOptions options)
        : this()
    {
        _options = options;
    }
    
    public bool Set(object key, T value)
    {
        if (_dictionary.Count >= _options.SizeLimit)
        {
            var pruned = TryPrune();
            if (!pruned)
                return false;
        }

        var success = false;
        _cacheLock.EnterWriteLock();
        try
        {
            success =_dictionary.TryAdd(key, value);
            
        }
        finally
        {
            _cacheLock.ExitWriteLock();
            if (success)
            {
                EnqueueKey(key);
            }
        }
        
        return success;
    }

    public T? GetOrSet(object key, Func<object,T> dataFetcher)
    {
        T? value;
        var success = false;
        _cacheLock.EnterUpgradeableReadLock();
        try
        {
            success = _dictionary.TryGetValue(key, out value);
            if (!success)
            {
                value = dataFetcher(key);
                success = Set(key, value);
            }
        }
        finally
        {
            _cacheLock.ExitUpgradeableReadLock();
        }
        
        return success ? value : default;
    }

    private void EnqueueKey(object key)
    {
        _queueLock.EnterWriteLock();
        try
        {
            _keyQueue.Enqueue(key);
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }
    }

    public bool TryGet(object key, out T value)
    {
        var success = false;
        _cacheLock.EnterReadLock();
        try
        {
            success = _dictionary.TryGetValue(key, out value!);
        }
        finally
        {
            _cacheLock.ExitReadLock();
            if (success && _options.EvictionPolicy == Evict.LeastRecentlyUsed)
                RefreshQueue(key);
        }
        
        return success;
    }

    private void RefreshQueue(object key)
    {
        _queueLock.EnterWriteLock();
        try
        {
            _keyQueue = new ConcurrentQueue<object>(_keyQueue.Where(x => !x.Equals(key)));
            _keyQueue.Enqueue(key);
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }
    }

    public bool TryPrune()
    {
        var success = false;
        _cacheLock.EnterWriteLock();
        _queueLock.EnterWriteLock();
        try
        {
            _keyQueue.TryDequeue(out var key);
            success = _dictionary.TryRemove(key!, out _);
        }
        finally
        {
            _cacheLock.ExitWriteLock();
            _queueLock.ExitWriteLock();
        }

        return success;
    }
}
namespace simplecache;

public class ConcurrentCache<T>
{
    private readonly Dictionary<object, T> _dictionary;
    private Queue<object> _keyQueue { get; set; } = new();
    private CacheOptions _options { get; init; } = new();
    private readonly ReaderWriterLockSlim _cacheLock = new();
    private readonly ReaderWriterLockSlim _queueLock = new();

    public ConcurrentCache()
    {
        _dictionary = new();
    }

    public ConcurrentCache(CacheOptions options)
        : this()
    {
        _options = options;
    }
    
    public int Count => _dictionary.Count;

    public bool Set(object key, T value)
    {
        _cacheLock.EnterWriteLock();
        if (_dictionary.Count >= _options.SizeLimit)
        {
            _queueLock.EnterWriteLock();
            var pruned = false;
            try
            {
                pruned = TryPrune();
            }
            finally
            {
                _queueLock.ExitWriteLock();
                if (!pruned)
                {
                    _cacheLock.ExitWriteLock();
                }                
            }
            if (!pruned)
                return false;
        }

        var success = false;
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
        bool success;
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
            _keyQueue = new (_keyQueue.Where(x => !x.Equals(key)));
            _keyQueue.Enqueue(key);
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }
    }

    public bool TryPrune()
    {
        _keyQueue.TryDequeue(out var key); 
        return _dictionary.Remove(key!, out _);
    }
}
namespace simplecache;

public interface IMemoryCache<T>
{
    public int Count { get; }
    public bool Set(object key, T value);
    public T? GetOrSet(object key, Func<object, T> dataFetcher);
    public bool TryGet(object key, out T value);
}
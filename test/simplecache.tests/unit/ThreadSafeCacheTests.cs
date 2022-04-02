using System.Collections.Concurrent;
using System.Collections.Generic;

namespace simplecache.tests.unit;

public class ThreadSafeCacheTests
{
    [Theory]
    [InlineData(1, "test value")]
    [InlineData(2, 42)]
    [InlineData("3", typeof(TimeOnly))]
    public void Add_ShouldAddObject_ToCache<T>(object key, T value)
    {
        var sut = new ThreadSafeCache<T>();

        sut.Set(key, value);

        var success = sut.TryGet(key, out var result);

        success.Should().BeTrue();
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(42)]
    [InlineData(2132)]
    public void Add_ShouldEvictOldestEntry_WhenSizeLimitIsReached(int sizeLimit)
    {
        var sut = new ThreadSafeCache<string>(new CacheOptions { SizeLimit = 2 });

        for (int i = 0; i <= sizeLimit; i++)
        {
            sut.Set(i, $"test entry #{i}");
        }

        var oldestEntry = sut.TryGet(0, out _);

        oldestEntry.Should().BeFalse();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(42)]
    [InlineData(2132)]
    public void Add_ShouldEvict_Only_OldestEntry_WhenSizeLimitIsReached(int sizeLimit)
    {
        var sut = new ThreadSafeCache<string>(new CacheOptions { SizeLimit = sizeLimit });

        for (int i = 0; i <= sizeLimit; i++)
        {
            sut.Set(i, $"test entry #{i}");
        }

        var oldestEntry = sut.TryGet(0, out _);
        var secondOldest = sut.TryGet(1, out var result);

        oldestEntry.Should().BeFalse();
        secondOldest.Should().BeTrue();
        result.Should().Match("test entry #1");
    }

    [Fact]
    public void New_ThreadSafeCacheWithoutOptions_ShouldHave_NoSizeLimit()
    {
        var sut = new ThreadSafeCache<string>();
        var result = sut.GetPrivateProperty<CacheOptions>("_options");
        
        result.SizeLimit.Should().BeNull();
    }

    [Fact]
    public void Add_ShouldEvict_LeastRecentlyUsedEntry_WhenThisOptionIsSet()
    {
        var sut = new ThreadSafeCache<string>(new CacheOptions { SizeLimit = 2, EvictionPolicy = Evict.LeastRecentlyUsed });

        sut.Set(1, "1");
        sut.Set(2, "2");
        sut.TryGet(1, out _);
        sut.Set(3, "3");

        var oldestEntry = sut.TryGet(1, out var result);
        var leastRecentEntry = sut.TryGet(2, out _);

        leastRecentEntry.Should().BeFalse();
        oldestEntry.Should().BeTrue();
        result.Should().Match("1");
    }

    [Theory]
    [InlineData(1, "test value")]
    [InlineData(2, 42)]
    [InlineData("3", typeof(TimeOnly))]
    public void GetOrSet_ShouldSetCache_WhenEntryIsNew<T>(object key, T value)
    {
        var sut = new ThreadSafeCache<T>();

        sut.GetOrSet(key, x => value);

        var success = sut.TryGet(key, out var result);

        success.Should().BeTrue();
        result.Should().Be(value);
    }

    [Fact]
    public void GetOrSet_ShouldSetOnlyOnce_WithMultipleParallelRequestForThesameObject()
    {
        for (int j = 0; j < 100_000; j++)
        {
            var sut = new ThreadSafeCache<string>();
            var cacheLog = new ConcurrentQueue<string>();

            Parallel.For(1, 9, (i) =>
            {
                var value = i.ToString();
                var cached = sut.GetOrSet(1, x => value);
                if (cached == value)
                {
                    cacheLog.Enqueue(value);
                }
            });

            sut.TryGet(1, out var result);
            cacheLog.TryDequeue(out var expected);
            
            result.Should().Match(expected);
            cacheLog.Count.Should().Be(0);
        }
    }
    
    
}
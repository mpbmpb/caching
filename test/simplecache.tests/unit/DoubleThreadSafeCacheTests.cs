using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Bogus;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace simplecache.tests.unit;

public class DoubleThreadSafeCacheTests
{
    public byte[][] FakeImgData;
    
    [Theory]
    [InlineData(1, "test value")]
    [InlineData(2, 42)]
    [InlineData("3", typeof(TimeOnly))]
    public void Set_ShouldAddObject_ToCache<T>(object key, T value)
    {
        var sut = new DoubleThreadSafeCache<T>();

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
    public void Set_ShouldEvictOldestEntry_WhenSizeLimitIsReached(int sizeLimit)
    {
        var sut = new DoubleThreadSafeCache<string>(new CacheOptions { SizeLimit = 2 });

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
    public void Set_ShouldEvict_Only_OldestEntry_WhenSizeLimitIsReached(int sizeLimit)
    {
        var sut = new DoubleThreadSafeCache<string>(new CacheOptions { SizeLimit = sizeLimit });

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
        var sut = new DoubleThreadSafeCache<string>();
        var result = sut.GetPrivateProperty<CacheOptions>("_options");
        
        result.SizeLimit.Should().BeNull();
    }

    [Fact]
    public void Set_ShouldEvict_LeastRecentlyUsedEntry_WhenThisOptionIsSet()
    {
        var sut = new DoubleThreadSafeCache<string>(new CacheOptions { SizeLimit = 2, EvictionPolicy = Evict.LeastRecentlyUsed });

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
        var sut = new DoubleThreadSafeCache<T>();

        sut.GetOrSet(key, x => value);

        var success = sut.TryGet(key, out var result);

        success.Should().BeTrue();
        result.Should().Be(value);
    }

    [Fact]
    public void GetOrSet_ShouldSetOnlyOnce_WithMultipleParallelRequestForThesameKey()
    {
        for (int j = 0; j < 10_000; j++)
        {
            var sut = new DoubleThreadSafeCache<string>();
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

    [Fact]
    public void Set_ShouldBe_Threadsafe()
    {
        for (int j = 0; j < 10_000; j++)
        {
            var sut = new DoubleThreadSafeCache<string>();
            var errorCount = 0;
            var cachedCount = 0;

            Parallel.For(1, 9, (i) =>
            {
                var success = sut.Set(i, i.ToString());
                if (!success)
                    Interlocked.Increment(ref errorCount);
            });

            for (int i = 1; i < 9; i++)
            {
                var success = sut.TryGet(i, out var value);
                if (!success)
                    errorCount++;
                if (value == i.ToString())
                    cachedCount++;
            }

            errorCount.Should().Be(0);
            cachedCount.Should().Be(8);
        }
    }
    
    [Fact]
    public void Set_ShouldBe_Threadsafe_WhenPruningCache()
    {
        for (int j = 0; j < 10_000; j++)
        {
            var sut = new DoubleThreadSafeCache<string>(new CacheOptions(){ SizeLimit = 8 });
            var errorCount = 0;
            var cachedCount = 0;

            for (int i = 11; i < 19; i++)
            {
                sut.Set(i, "test value");
            }
            
            Parallel.For(1, 9, (i) =>
            {
                var success = sut.Set(i, i.ToString());
                if (!success)
                    Interlocked.Increment(ref errorCount);
            });

            for (int i = 1; i < 9; i++)
            {
                var success = sut.TryGet(i, out var value);
                if (!success)
                    errorCount++;
                if (value == i.ToString())
                    cachedCount++;
            }

            errorCount.Should().Be(0);
            cachedCount.Should().Be(8);
            sut.Count.Should().Be(8);
        }
    }

    [Fact]
    public void Multithreaded_StressTest_AllCaches_ShouldBe_SetCorrectly()
    {
        StressTestSetup();
        var sut = new DoubleThreadSafeCache<byte[]>(new CacheOptions(){ SizeLimit = 5_000 });
        var success = false;

        Parallel.For(0, 5_000, (i) =>
        {
            success = sut.Set(i, FakeImgData[i]);
            if (!success) 
                throw new TestCanceledException("A failure occurred trying to set the cache, the test has failed.");
        });

        sut.Count.Should().Be(5_000);
        
        // The next part will test thread safety while setting a cache that prunes itself
        Parallel.For(5_000, 10_000, (i) =>
        {
            success = sut.Set(i, FakeImgData[i]);
            if (!success) throw new TestCanceledException("A failure occurred trying to set the cache, the test has failed.");
        });
        
        sut.Count.Should().Be(5_000);

        var checkList = new List<byte[]>();

        for (int i = 5_000; i < 10_000; i++)
        {
            success = sut.TryGet(i, out var value);
            if (!success) 
                throw new TestCanceledException($"A failure occurred trying to get the cache for key {i}, the test has failed.");
            checkList.Add(value);
        }

        checkList.Should().BeEquivalentTo(FakeImgData[5_000..]);
    }

    private void StressTestSetup()
    {
        FakeImgData = new byte[10_000][];
        var rnd = new Random();

        for (int i = 0; i < 10_000; i++)
        {
            var arr = new byte[180_000];
            rnd.NextBytes(arr);
            FakeImgData[i] = arr;
        }
    }
}
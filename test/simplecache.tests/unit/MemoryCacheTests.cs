using System.Reflection;

namespace simplecache.tests.unit;

public class MemoryCacheTests
{
    [Theory]
    [InlineData(1, "test value")]
    [InlineData(2, 42)]
    [InlineData("3", typeof(TimeOnly))]
    public void Add_ShouldAddObject_ToCache<T>(object key, T value)
    {
        var sut = new MemoryCache<T>();

        sut.Add(key, value);

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
        var sut = new MemoryCache<string>(new CacheOptions { SizeLimit = 2 });

        for (int i = 0; i <= sizeLimit; i++)
        {
            sut.Add(i, $"test entry #{i}");
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
        var sut = new MemoryCache<string>(new CacheOptions { SizeLimit = sizeLimit });

        for (int i = 0; i <= sizeLimit; i++)
        {
            sut.Add(i, $"test entry #{i}");
        }

        var oldestEntry = sut.TryGet(0, out _);
        var secondOldest = sut.TryGet(1, out var result);

        oldestEntry.Should().BeFalse();
        secondOldest.Should().BeTrue();
        result.Should().Match("test entry #1");
    }

    [Fact]
    public void New_MemoryCacheWithoutOptions_ShouldHave_NoSizeLimit()
    {
        var sut = new MemoryCache<string>();
        var result = sut.GetPrivateProperty<CacheOptions>("_options");
        
        result.SizeLimit.Should().BeNull();
    }

    [Fact]
    public void Add_ShouldEvict_LeastRecentlyUsedEntry_WhenThisOptionIsSet()
    {
        var sut = new MemoryCache<string>(new CacheOptions { SizeLimit = 2, EvictionPolicy = Evict.LeastRecentlyUsed });

        sut.Add(1, "1");
        sut.Add(2, "2");
        sut.TryGet(1, out _);
        sut.Add(3, "3");

        var oldestEntry = sut.TryGet(1, out var result);
        var leastRecentEntry = sut.TryGet(2, out _);

        leastRecentEntry.Should().BeFalse();
        oldestEntry.Should().BeTrue();
        result.Should().Match("1");
    }

}
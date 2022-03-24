using System.Reflection;

namespace simplecache.tests.unit;

public class MemoryCacheTests
{
    [Theory]
    [InlineData(1, "test value")]
    [InlineData(2, 42)]
    [InlineData("3", typeof(TimeOnly))]
    public async Task Add_ShouldAddObject_ToCache<T>(object key, T value)
    {
        var sut = new MemoryCache<T>();

        await sut.Add(key, value);

        var result = await sut.TryGet(key);

        result.Should().Be((true, value));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(42)]
    [InlineData(2132)]
    public async Task Add_ShouldEvictOldestEntry_WhenSizeLimitIsReached(int sizeLimit)
    {
        var sut = new MemoryCache<string>(new CacheOptions {SizeLimit = 2});

        for (int i = 0; i <= sizeLimit; i++)
        {
            await sut.Add(i, $"test entry #{i}");
        }

        var oldestEntry = await sut.TryGet(0);

        oldestEntry.Item1.Should().BeFalse();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(42)]
    [InlineData(2132)]
    public async Task Add_ShouldEvict_Only_OldestEntry_WhenSizeLimitIsReached(int sizeLimit)
    {
        var sut = new MemoryCache<string>(new CacheOptions {SizeLimit = sizeLimit});

        for (int i = 0; i <= sizeLimit; i++)
        {
            await sut.Add(i, $"test entry #{i}");
        }

        var oldestEntry = await sut.TryGet(0);
        var secondOldest = await sut.TryGet(1);

        oldestEntry.Item1.Should().BeFalse();
        secondOldest.Item1.Should().BeTrue();
    }

    [Fact]
    public async Task New_MemoryCacheWithoutOptions_ShouldHave_NoSizeLimit()
    {
        var sut = new MemoryCache<string>();
        var result = sut.GetPrivateProperty<CacheOptions>("_options");
        
        result.SizeLimit.Should().BeNull();
    }

    
}
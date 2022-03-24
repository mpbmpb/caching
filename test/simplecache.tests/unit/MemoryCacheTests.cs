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

    [Fact]
    public async Task Add_ShouldEvictOldestEntry_WhenSizeLimitIsReached()
    {
        var sut = new MemoryCache<string>(new CacheOptions {SizeLimit = 2});

        await sut.Add(1, "1");
        await sut.Add(2, "2");
        await sut.Add(3, "3");

        var result = await sut.TryGet(1);

        result.Item1.Should().BeFalse();
    }


}
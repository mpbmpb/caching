using Bogus;
using Microsoft.Extensions.Caching.Memory;
using simplecache;

namespace benchmarks;

[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CachingBenchmarks
{
    public List<byte[]> FakeImgData = new();
    public List<string> FakeUrls = new();
    public MemoryCache MSCache;
    public MemoryCache<byte[]> ImgCache;
    public MemoryCache<string> UrlCache;

    [GlobalSetup]
    public void GlobalSetup()
    {
        Random rnd = new Random();

        for (int i = 0; i < 1000; i++)
        {
            var arr = new byte[180_000];
            rnd.NextBytes(arr);
            FakeImgData.Add(arr);
        }

        var faker = new Faker();
        
        for (int i = 0; i < 1000; i++)
        {
            FakeUrls.Add(faker.Internet.Url());
        }
    }

    [Benchmark]
    public void MSMemoryCache_SetImages()
    {
        MSCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });

        for (int i = 0; i < 1000; i++)
        {
            MSCache.Set(i, FakeImgData[i], new MemoryCacheEntryOptions().SetSize(1));
        }
    }

    [Benchmark]
    public void MyMemoryCache_SetImages()
    {
        ImgCache = new MemoryCache<byte[]>( new CacheOptions { SizeLimit = 1000 });
        
        for (int i = 0; i < 1000; i++)
        {
            ImgCache.Set(i, FakeImgData[i]);
        }
    }

    [Benchmark]
    public void MSMemoryCache_SetUrls()
    {
        MSCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });

        for (int i = 0; i < 1000; i++)
        {
            MSCache.Set(i, FakeUrls[i], new MemoryCacheEntryOptions().SetSize(1));
        }
    }

    [Benchmark]
    public void MyMemoryCache_SetUrls()
    {
        UrlCache = new MemoryCache<string>( new CacheOptions { SizeLimit = 1000 });
        
        for (int i = 0; i < 1000; i++)
        {
            UrlCache.Set(i, FakeUrls[i]);
        }
    }

}
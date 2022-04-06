using Bogus;
using Microsoft.Extensions.Caching.Memory;
using simplecache;

namespace benchmarks;

[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CachingWithPruningBenchmarks
{
    public List<byte[]> FakeImgData;
    public List<string> FakeUrls;
    public MemoryCache MSCache;
    public MemoryCache<byte[]> MemoryImgCache;
    public MemoryCache<string> MemoryUrlCache;
    public ConcurrentCache<byte[]> ConcurrentImgCache;
    public ConcurrentCache<string> ConcurrentUrlCache;


    [Params(1000)] 
    public int Inserts { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        FakeImgData = new(Inserts);
        FakeUrls = new(Inserts);
        Random rnd = new Random();

        for (int i = 0; i < Inserts * 2; i++)
        {
            var arr = new byte[180_000];
            rnd.NextBytes(arr);
            FakeImgData.Add(arr);
        }

        var faker = new Faker();
        
        for (int i = 0; i < Inserts * 2; i++)
        {
            FakeUrls.Add(faker.Internet.Url());
        }
        
        MSCache = new(new MemoryCacheOptions { SizeLimit = Inserts });
        MemoryImgCache = new( new CacheOptions { SizeLimit = Inserts });
        MemoryUrlCache = new( new CacheOptions { SizeLimit = Inserts });
        ConcurrentImgCache = new(new CacheOptions { SizeLimit = Inserts });
        ConcurrentUrlCache = new(new CacheOptions { SizeLimit = Inserts });
        
        for (int i = Inserts; i < Inserts * 2; i++)
        {
            MSCache.Set(i, FakeImgData[i], new MemoryCacheEntryOptions().SetSize(1));
            MemoryImgCache.Set(i, FakeImgData[i]);
            MemoryUrlCache.Set(i, FakeUrls[i]);
        }
    }

    [Benchmark]
    public void MSMemoryCache_SetImages()
    {
        for (int i = 0; i < Inserts; i++)
        {
            MSCache.Set(i, FakeImgData[i], new MemoryCacheEntryOptions().SetSize(1));
        }
    }
    
    [Benchmark]
    public void MSMemoryCache_SetUrls()
    {
        for (int i = 0; i < Inserts; i++)
        {
            MSCache.Set(i, FakeUrls[i], new MemoryCacheEntryOptions().SetSize(1));
        }
    }
    
    [Benchmark]
    public void MemoryCache_SetImages()
    {
        for (int i = 0; i < Inserts; i++)
        {
            MemoryImgCache.Set(i, FakeImgData[i]);
        }
    }
    
    [Benchmark]
    public void MemoryCache_SetUrls()
    {
        for (int i = 0; i < Inserts; i++)
        {
            MemoryUrlCache.Set(i, FakeUrls[i]);
        }
    }

    [Benchmark]
    public void ConcurrentCache_SetImages()
    {
        for (int i = 0; i < Inserts; i++)
        {
            ConcurrentImgCache.Set(i, FakeImgData[i]);
        }
    }
    
    [Benchmark]
    public void ConcurrentCache_SetUrls()
    {
        for (int i = 0; i < Inserts; i++)
        {
            ConcurrentUrlCache.Set(i, FakeUrls[i]);
        }
    }

}
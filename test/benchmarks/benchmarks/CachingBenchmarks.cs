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
    public IMemoryCache<byte[]> ImgCache;
    public IMemoryCache<string> UrlCache;
    public byte[] ImgDump;
    public string UrlDump;


    [Params(1000)] 
    public int Inserts { get; set; }

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
        MSCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = Inserts });
    
        for (int i = 0; i < Inserts; i++)
        {
            MSCache.Set(i, FakeImgData[i], new MemoryCacheEntryOptions().SetSize(1));
        }
    }
    
    [Benchmark]
    public void MSMemoryCache_SetUrls()
    {
        MSCache = new (new MemoryCacheOptions { SizeLimit = Inserts });
    
        for (int i = 0; i < Inserts; i++)
        {
            MSCache.Set(i, FakeUrls[i], new MemoryCacheEntryOptions().SetSize(1));
        }
    }
    
    [Benchmark]
    public void MemoryCache_SetImages()
    {
        ImgCache = new MemoryCache<byte[]>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < Inserts; i++)
        {
            ImgCache.Set(i, FakeImgData[i]);
        }
    }
    
    [Benchmark]
    public void MemoryCache_SetUrls()
    {
        UrlCache = new MemoryCache<string>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < Inserts; i++)
        {
            UrlCache.Set(i, FakeUrls[i]);
        }
    }
    
    [Benchmark]
    public void DoubleThreadSafeCache_SetImages()
    {
        ImgCache = new DoubleThreadSafeCache<byte[]>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < Inserts; i++)
        {
            ImgCache.Set(i, FakeImgData[i]);
        }
    }
    
    [Benchmark]
    public void DoubleThreadSafeCache_SetUrls()
    {
        UrlCache = new DoubleThreadSafeCache<string>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < Inserts; i++)
        {
            UrlCache.Set(i, FakeUrls[i]);
        }
    }
    
    [Benchmark]
    public void ConcurrentCache_SetImages()
    {
        ImgCache = new ConcurrentCache<byte[]>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < Inserts; i++)
        {
            ImgCache.Set(i, FakeImgData[i]);
        }
    }
    
    [Benchmark]
    public void ConcurrentCache_SetUrls()
    {
        UrlCache = new ConcurrentCache<string>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < Inserts; i++)
        {
            UrlCache.Set(i, FakeUrls[i]);
        }
    }

    [GlobalSetup(Target = nameof(MSMemoryCache_GetImages))]
    public void MS_ImagesSetup()
    {
        GlobalSetup();
        MSCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = Inserts });

        for (int i = 0; i < 1000; i++)
        {
            MSCache.Set(i, FakeImgData[i], new MemoryCacheEntryOptions().SetSize(1));
        }
    }

    [Benchmark]
    public void MSMemoryCache_GetImages()
    {
        for (int i = 0; i < 1000; i++)
        {
            MSCache.Get(i);
        }
    }

    [GlobalSetup(Target = nameof(MSMemoryCache_GetUrls))]
    public void MS_UrlSetup()
    {
        GlobalSetup();
        MSCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = Inserts });

        for (int i = 0; i < 1000; i++)
        {
            MSCache.Set(i, FakeUrls[i], new MemoryCacheEntryOptions().SetSize(1));
        }
    }

    [Benchmark]
    public void MSMemoryCache_GetUrls()
    {
        for (int i = 0; i < 1000; i++)
        {
            MSCache.Get(i);
        }
    }

    [GlobalSetup(Target = nameof(MemoryCache_GetImages))]
    public void MemoryCacheImgSetup()
    {
        GlobalSetup();
        ImgCache = new MemoryCache<byte[]>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < 1000; i++)
        {
            ImgCache.Set(i, FakeImgData[i]);
        }
    }

    [Benchmark]
    public void MemoryCache_GetImages()
    {
        for (int i = 0; i < 1000; i++)
        {
            ImgCache.TryGet(i, out ImgDump);
        }
    }

    [GlobalSetup(Target = nameof(MemoryCache_GetUrls))]
    public void MemoryCacheUrlSetup()
    {
        GlobalSetup();
        UrlCache = new MemoryCache<string>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < 1000; i++)
        {
            UrlCache.Set(i, FakeUrls[i]);
        }
    }
    
    [Benchmark]
    public void MemoryCache_GetUrls()
    {
        for (int i = 0; i < 1000; i++)
        {
            UrlCache.TryGet(i, out UrlDump);
        }
    }

    [GlobalSetup(Target = nameof(DoubleThreadSafeCache_GetImages))]
    public void DoubleThreadSafeCacheImgSetup()
    {
        GlobalSetup();
        ImgCache = new DoubleThreadSafeCache<byte[]>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < 1000; i++)
        {
            ImgCache.Set(i, FakeImgData[i]);
        }
    }
    
    [Benchmark]
    public void DoubleThreadSafeCache_GetImages()
    {
        for (int i = 0; i < 1000; i++)
        {
            ImgCache.TryGet(i, out ImgDump);
        }
    }

    [GlobalSetup(Target = nameof(DoubleThreadSafeCache_GetUrls))]
    public void DoubleThreadSafeCacheUrlSetup()
    {
        GlobalSetup();
        UrlCache = new DoubleThreadSafeCache<string>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < 1000; i++)
        {
            UrlCache.Set(i, FakeUrls[i]);
        }
    }

    [Benchmark]
    public void DoubleThreadSafeCache_GetUrls()
    {
        for (int i = 0; i < 1000; i++)
        {
            UrlCache.TryGet(i, out UrlDump);
        }
    }
    
    [GlobalSetup(Target = nameof(ConcurrentCache_GetImages))]
    public void ConcurrentCacheImgSetup()
    {
        GlobalSetup();
        ImgCache = new ConcurrentCache<byte[]>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < 1000; i++)
        {
            ImgCache.Set(i, FakeImgData[i]);
        }
    }
    
    [Benchmark]
    public void ConcurrentCache_GetImages()
    {
        for (int i = 0; i < 1000; i++)
        {
            ImgCache.TryGet(i, out ImgDump);
        }
    }
    
    [GlobalSetup(Target = nameof(ConcurrentCache_GetUrls))]
    public void ConcurrentCacheUrlSetup()
    {
        GlobalSetup();
        UrlCache = new ConcurrentCache<string>( new CacheOptions { SizeLimit = Inserts });
        
        for (int i = 0; i < 1000; i++)
        {
            UrlCache.Set(i, FakeUrls[i]);
        }
    }

    [Benchmark]
    public void ConcurrentCache_GetUrls()
    {
        for (int i = 0; i < 1000; i++)
        {
            UrlCache.TryGet(i, out UrlDump);
        }
    }
    
}
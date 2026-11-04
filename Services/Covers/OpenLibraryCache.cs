using Microsoft.Extensions.Caching.Memory;

namespace MyCatalog.Services.Covers;

public sealed class OpenLibraryCache : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 100 });
    public bool TryGet<T>(string key, out T? value) => _cache.TryGetValue(key, out value);
    public void Set<T>(string key, T value, bool empty) => _cache.Set(key, value,
        new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(empty ? 5 : 30) });
    public void Dispose() => _cache.Dispose();
}

using System.Text.Json;
using BookingSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace BookingSystem.Infrastructure.Caching;

// Implementa ICacheService sopra IDistributedCache, l'astrazione standard di ASP.NET
// Core per cache distribuite (Program.cs la configura con AddStackExchangeRedisCache,
// quindi qui gira su Redis, ma il codice di questa classe non lo sa esplicitamente).
// I valori sono serializzati in JSON perché IDistributedCache lavora solo con
// stringhe/byte[], non con oggetti .NET.
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var cached = await _cache.GetStringAsync(key);
        if (cached == null)
            return default;

        return JsonSerializer.Deserialize<T>(cached);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });
    }

    public Task RemoveAsync(string key)
    {
        return _cache.RemoveAsync(key);
    }
}

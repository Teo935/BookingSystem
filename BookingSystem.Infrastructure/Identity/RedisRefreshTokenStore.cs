using BookingSystem.Application.Interfaces;
using StackExchange.Redis;

namespace BookingSystem.Infrastructure.Identity;

// Implementazione di IRefreshTokenStore su Redis: chiave "refreshtoken:{token}" con
// valore l'userId, e scadenza (TTL) affidata direttamente a Redis invece di un campo
// "ExpiresAt" da controllare a mano — quando il TTL scade, la entry sparisce da sola.
// Usa IConnectionMultiplexer/IDatabase direttamente (non ICacheService, la stessa
// astrazione usata per la cache delle Room) perché qui serve un delete esplicito e
// atomico per la rotazione del token, non un semplice get/set generico.
public class RedisRefreshTokenStore : IRefreshTokenStore
{
    private const string KeyPrefix = "refreshtoken:";

    private readonly IConnectionMultiplexer _redis;

    public RedisRefreshTokenStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task StoreAsync(string token, string userId, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        return db.StringSetAsync(KeyPrefix + token, userId, ttl);
    }

    public async Task<string?> GetUserIdAsync(string token)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(KeyPrefix + token);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public Task RemoveAsync(string token)
    {
        var db = _redis.GetDatabase();
        return db.KeyDeleteAsync(KeyPrefix + token);
    }
}

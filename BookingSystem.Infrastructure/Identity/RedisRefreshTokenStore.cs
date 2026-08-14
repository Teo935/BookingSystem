using BookingSystem.Application.Interfaces;
using StackExchange.Redis;

namespace BookingSystem.Infrastructure.Identity;

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

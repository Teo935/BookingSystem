using BookingSystem.Application.Interfaces;
using StackExchange.Redis;

namespace BookingSystem.Infrastructure.RateLimiting;

public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimiter(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
    {
        var db = _redis.GetDatabase();

        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(key, window);
        }

        return count <= limit;
    }
}

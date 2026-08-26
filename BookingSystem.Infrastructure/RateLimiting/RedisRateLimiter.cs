using BookingSystem.Application.Interfaces;
using StackExchange.Redis;

namespace BookingSystem.Infrastructure.RateLimiting;

// Algoritmo fixed-window counter: per ogni chiave (es. "ratelimit:login:1.2.3.4") si
// incrementa un contatore su Redis; al primo hit della finestra si imposta anche la
// scadenza (EXPIRE), così il contatore si azzera da solo quando la finestra scade.
// INCR è atomico lato server Redis: anche con richieste concorrenti sulla stessa
// chiave non si verificano race condition, a differenza di un get-poi-set fatto a
// mano (leggi valore, controlla, scrivi — tra le due operazioni un'altra richiesta
// potrebbe intromettersi).
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
            // La scadenza va impostata solo al primo hit (count == 1): sui successivi
            // non va toccata, altrimenti la finestra si allungherebbe ad ogni richiesta
            // invece di restare fissa dal primo hit (da cui "fixed-window").
            await db.KeyExpireAsync(key, window);
        }

        return count <= limit;
    }
}

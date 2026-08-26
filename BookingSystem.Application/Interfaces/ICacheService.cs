namespace BookingSystem.Application.Interfaces;

// Astrazione di cache generica: l'Application layer conosce solo Get/Set/Remove su un
// valore serializzabile, non sa (né deve sapere) che dietro c'è Redis. L'unica
// implementazione oggi è RedisCacheService in Infrastructure, ma qualunque cache (Redis,
// in-memory, ecc.) potrebbe sostituirla senza toccare chi la usa.
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
}

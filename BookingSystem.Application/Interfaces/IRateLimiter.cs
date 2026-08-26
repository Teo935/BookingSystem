namespace BookingSystem.Application.Interfaces;

// Contratto astratto per il rate limiting: "data una chiave (es. IP o userId), sono
// ancora entro il limite di richieste nella finestra di tempo?". L'implementazione
// concreta (RedisRateLimiter) usa un contatore fixed-window su Redis, ma questa
// interfaccia non lo rivela: potrebbe girare anche in-memory per un solo server.
public interface IRateLimiter
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
}

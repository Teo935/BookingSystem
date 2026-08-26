namespace BookingSystem.Application.Interfaces;

// Astrazione per la persistenza dei refresh token (opachi, non JWT): un token deve poter
// essere revocato lato server in ogni momento (logout, rotazione), cosa che un JWT
// autofirmato non permetterebbe senza una blocklist. L'implementazione concreta
// (RedisRefreshTokenStore) sfrutta il TTL nativo di Redis per la scadenza automatica.
public interface IRefreshTokenStore
{
    Task StoreAsync(string token, string userId, TimeSpan ttl);
    Task<string?> GetUserIdAsync(string token);
    Task RemoveAsync(string token);
}

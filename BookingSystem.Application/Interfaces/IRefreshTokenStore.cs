namespace BookingSystem.Application.Interfaces;

public interface IRefreshTokenStore
{
    Task StoreAsync(string token, string userId, TimeSpan ttl);
    Task<string?> GetUserIdAsync(string token);
    Task RemoveAsync(string token);
}

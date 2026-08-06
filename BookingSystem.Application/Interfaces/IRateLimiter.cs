namespace BookingSystem.Application.Interfaces;

public interface IRateLimiter
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
}

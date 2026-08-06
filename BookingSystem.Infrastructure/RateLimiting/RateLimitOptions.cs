namespace BookingSystem.Infrastructure.RateLimiting;

public class RateLimitPolicy
{
    public int Limit { get; set; }
    public int WindowSeconds { get; set; }
}

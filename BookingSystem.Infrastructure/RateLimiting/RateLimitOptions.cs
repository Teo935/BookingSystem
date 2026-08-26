namespace BookingSystem.Infrastructure.RateLimiting;

// Una policy nominata (es. "Login", "Register", "CreateBooking") letta dalla sezione
// "RateLimiting" di appsettings.json come Dictionary<string, RateLimitPolicy> — ogni
// endpoint protetto referenzia una policy per nome tramite [RateLimit("Login", ...)].
public class RateLimitPolicy
{
    public int Limit { get; set; }
    public int WindowSeconds { get; set; }
}

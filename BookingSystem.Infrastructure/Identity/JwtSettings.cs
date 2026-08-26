namespace BookingSystem.Infrastructure.Identity;

// POCO (Plain Old CLR Object) di configurazione, popolato da IConfiguration (sezione
// "Jwt" di appsettings.json / User Secrets) e passato via Dependency Injection.
// SecretKey è un segreto vero e proprio (non va mai committato in chiaro), le altre
// proprietà sono configurazione ordinaria.
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
}

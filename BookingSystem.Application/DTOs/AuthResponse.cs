namespace BookingSystem.Application.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    // Nullable perché Register non genera un refresh token (solo Login/Refresh lo fanno).
    public string? RefreshToken { get; set; }
    public string Email { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
}

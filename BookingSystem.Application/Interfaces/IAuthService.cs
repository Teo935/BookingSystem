using BookingSystem.Application.DTOs;

namespace BookingSystem.Application.Interfaces;

// Contratto per il flusso di autenticazione. L'implementazione (AuthService, in
// Infrastructure) è quella che tocca ASP.NET Identity (UserManager/RoleManager) e genera
// i token JWT — qui nell'Application layer si vede solo "cosa" fa, non "come".
public interface IAuthService
{
    // Register non genera un refresh token (solo Login/Refresh lo fanno).
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);

    // Consuma il refresh token esistente e ne emette uno nuovo (token rotation): il
    // vecchio smette di funzionare anche se non è scaduto.
    Task<(bool Success, string? Error, AuthResponse? Response)> RefreshAsync(string refreshToken);

    // Invalida un solo refresh token (una sessione/dispositivo), non tutte le sessioni
    // dell'utente. Idempotente: chiamarlo su un token già rimosso non genera errori.
    Task LogoutAsync(string refreshToken);
}

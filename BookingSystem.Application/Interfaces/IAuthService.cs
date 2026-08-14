using BookingSystem.Application.DTOs;

namespace BookingSystem.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}

using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Identity;

// Implementazione concreta di IAuthService: unico punto del progetto che usa
// UserManager/RoleManager di ASP.NET Identity (gestiscono hashing password,
// validazione, storage utenti) combinandoli con JwtTokenGenerator (Access Token) e
// IRefreshTokenStore (persistenza del refresh token su Redis).
public class AuthService : IAuthService
{
    private const string DefaultRole = Roles.User;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenStore _refreshTokenStore;

    public AuthService(UserManager<ApplicationUser> userManager, JwtTokenGenerator tokenGenerator, IRefreshTokenStore refreshTokenStore)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
        _refreshTokenStore = refreshTokenStore;
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, error, null);
        }

        await _userManager.AddToRoleAsync(user, DefaultRole);

        var response = BuildAuthResponse(user, new[] { DefaultRole });
        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, "Invalid email or password.", null);
        }

        // Messaggio di errore identico sia per "utente inesistente" che per "password
        // sbagliata" (sopra e qui sotto): evita di rivelare a un attaccante se una email
        // è registrata nel sistema (user enumeration).
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return (false, "Invalid email or password.", null);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = await BuildAuthResponseWithRefreshTokenAsync(user, roles);
        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> RefreshAsync(string refreshToken)
    {
        var userId = await _refreshTokenStore.GetUserIdAsync(refreshToken);
        if (userId == null)
        {
            return (false, "Invalid or expired refresh token.", null);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await _refreshTokenStore.RemoveAsync(refreshToken);
            return (false, "Invalid or expired refresh token.", null);
        }

        // Token rotation: il vecchio refresh token viene invalidato prima che il nuovo venga emesso.
        await _refreshTokenStore.RemoveAsync(refreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        var response = await BuildAuthResponseWithRefreshTokenAsync(user, roles);
        return (true, null, response);
    }

    // Rimuove solo il refresh token passato (una sessione/dispositivo), non tutti quelli
    // dell'utente — ogni login/refresh crea una entry Redis indipendente. Idempotente:
    // rimuovere una chiave Redis inesistente non genera errori.
    public Task LogoutAsync(string refreshToken)
    {
        return _refreshTokenStore.RemoveAsync(refreshToken);
    }

    private AuthResponse BuildAuthResponse(ApplicationUser user, IEnumerable<string> roles)
    {
        var rolesList = roles.ToList();
        var (token, expiresAt) = _tokenGenerator.GenerateToken(user, rolesList);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email ?? string.Empty,
            Roles = rolesList
        };
    }

    private async Task<AuthResponse> BuildAuthResponseWithRefreshTokenAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var response = BuildAuthResponse(user, roles);

        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        await _refreshTokenStore.StoreAsync(refreshToken, user.Id, _tokenGenerator.RefreshTokenExpiration);
        response.RefreshToken = refreshToken;

        return response;
    }
}

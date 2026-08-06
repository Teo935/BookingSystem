using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private const string DefaultRole = Roles.User;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthService(UserManager<ApplicationUser> userManager, JwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
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

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return (false, "Invalid email or password.", null);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = BuildAuthResponse(user, roles);
        return (true, null, response);
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
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingSystem.API.Filters;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;

namespace BookingSystem.API.Controllers;

// Endpoint pubblici (nessun token richiesto per usarli, ovviamente — [AllowAnonymous]
// a livello di classe). Register/Login/Refresh hanno un rate limit dedicato per
// mitigare tentativi di forza bruta e registrazioni massive automatizzate; Logout no,
// dato che opera solo su un token già noto al chiamante.
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [RateLimit("Register", RateLimitKeyType.IpAddress)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, error, response) = await _authService.RegisterAsync(request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
    }

    [HttpPost("login")]
    [RateLimit("Login", RateLimitKeyType.IpAddress)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, response) = await _authService.LoginAsync(request);

        if (!success)
        {
            return Unauthorized(new { error });
        }

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var (success, error, response) = await _authService.RefreshAsync(request.RefreshToken);

        if (!success)
        {
            return Unauthorized(new { error });
        }

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);

        return NoContent();
    }
}

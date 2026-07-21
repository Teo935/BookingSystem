using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;

namespace BookingSystem.API.Controllers;

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
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, response) = await _authService.LoginAsync(request);

        if (!success)
        {
            return Unauthorized(new { error });
        }

        return Ok(response);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingSystem.API.Filters;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;

namespace BookingSystem.API.Controllers;

// [Authorize] a livello di classe protegge tutte le action per default; i singoli
// endpoint che devono restare pubblici usano [AllowAnonymous] esplicito (vedi
// CheckAvailability sotto) — scelta "secure by default" invece del contrario.
[ApiController]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("api/rooms/{roomId}/availability")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAvailability(int roomId, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
    {
        var available = await _bookingService.IsRoomAvailableAsync(roomId, checkIn, checkOut);

        return Ok(new { available });
    }

    // Rate Limiting per userId (non per IP: qui l'utente è già autenticato, e l'IP non
    // sarebbe affidabile come chiave dietro NAT/proxy condivisi). userId viene letto
    // dal claim NameIdentifier del JWT già validato dal middleware di autenticazione —
    // il "!" è sicuro perché [Authorize] garantisce che la richiesta sia autenticata.
    [HttpPost("api/bookings")]
    [RateLimit("CreateBooking", RateLimitKeyType.UserId)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error, booking) = await _bookingService.CreateBookingAsync(request, userId);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(booking);
    }

    [HttpGet("api/bookings/mine")]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var bookings = await _bookingService.GetBookingsByUserAsync(userId);

        return Ok(bookings);
    }

    // Estrae userId/isAdmin dal token JWT e lascia che sia il Service a decidere
    // l'esito dell'autorizzazione (BookingAccessResult) — il Controller si limita a
    // tradurlo in HTTP (404/403/200). StatusCode(403) esplicito invece di Forbid():
    // Forbid() dipenderebbe implicitamente da quale authentication scheme risulta di
    // default, e questo progetto ne registra due (Identity/cookie e JWT Bearer).
    [HttpGet("api/bookings/{id}")]
    public async Task<IActionResult> GetBooking(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var (result, booking) = await _bookingService.GetBookingAsync(id, userId, isAdmin);

        return result switch
        {
            BookingAccessResult.NotFound => NotFound(),
            BookingAccessResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden),
            _ => Ok(booking)
        };
    }

    [HttpDelete("api/bookings/{id}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var result = await _bookingService.CancelBookingAsync(id, userId, isAdmin);

        return result switch
        {
            BookingAccessResult.NotFound => NotFound(),
            BookingAccessResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden),
            _ => NoContent()
        };
    }
}

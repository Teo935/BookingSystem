using Microsoft.AspNetCore.Mvc;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;

namespace BookingSystem.API.Controllers;

[ApiController]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("api/rooms/{roomId}/availability")]
    public async Task<IActionResult> CheckAvailability(int roomId, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
    {
        var available = await _bookingService.IsRoomAvailableAsync(roomId, checkIn, checkOut);

        return Ok(new { available });
    }

    [HttpPost("api/bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var (success, error, booking) = await _bookingService.CreateBookingAsync(request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(booking);
    }

    [HttpGet("api/bookings/{id}")]
    public async Task<IActionResult> GetBooking(int id)
    {
        var booking = await _bookingService.GetBookingAsync(id);

        if (booking == null) return NotFound();

        return Ok(booking);
    }

    [HttpDelete("api/bookings/{id}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var cancelled = await _bookingService.CancelBookingAsync(id);

        if (!cancelled) return NotFound();

        return NoContent();
    }
}

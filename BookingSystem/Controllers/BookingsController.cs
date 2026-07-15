using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingSystem.Data;
using BookingSystem.Models;
using BookingSystem.Services;

namespace BookingSystem.Controllers;

[ApiController]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly BookingService _bookingService;

    public BookingsController(AppDbContext db, BookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    [HttpGet("api/rooms/{roomId}/availability")]
    public async Task<IActionResult> CheckAvailability(int roomId, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
    {
        var hasOverlap = await _db.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            checkIn < b.CheckOut &&
            checkOut > b.CheckIn);

        return Ok(new { available = !hasOverlap });
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
        var booking = await _db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return NotFound();

        return Ok(booking);
    }

    [HttpDelete("api/bookings/{id}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
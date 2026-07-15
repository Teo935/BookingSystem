using Microsoft.EntityFrameworkCore;
using BookingSystem.Data;
using BookingSystem.Models;

namespace BookingSystem.Services;

public class BookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string? Error, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request)
    {
        if (request.CheckIn >= request.CheckOut)
        {
            return (false, "CheckIn date must be before CheckOut date.", null);
        }

        var roomExists = await _db.Rooms.AnyAsync(r => r.Id == request.RoomId);
        if (!roomExists)
        {
            return (false, "Room does not exist.", null);
        }

        bool hasOverlap = await _db.Bookings.AnyAsync(b =>
            b.RoomId == request.RoomId &&
            request.CheckIn < b.CheckOut &&
            request.CheckOut > b.CheckIn);

        if (hasOverlap)
        {
            return (false, "Room is already booked for the selected dates.", null);
        }

        var booking = new Booking
        {
            RoomId = request.RoomId,
            GuestName = request.GuestName,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        return (true, null, booking);
    }
}
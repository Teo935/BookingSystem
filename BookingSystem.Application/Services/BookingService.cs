using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;

    public BookingService(IBookingRepository bookingRepository, IRoomRepository roomRepository)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
    }

    public async Task<(bool Success, string? Error, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request, string userId)
    {
        if (request.CheckIn >= request.CheckOut)
        {
            return (false, "CheckIn date must be before CheckOut date.", null);
        }

        var room = await _roomRepository.GetByIdAsync(request.RoomId);
        if (room == null)
        {
            return (false, "Room does not exist.", null);
        }

        var hasOverlap = await _bookingRepository.HasOverlapAsync(request.RoomId, request.CheckIn, request.CheckOut);
        if (hasOverlap)
        {
            return (false, "Room is already booked for the selected dates.", null);
        }

        var booking = new Booking
        {
            RoomId = request.RoomId,
            UserId = userId,
            GuestName = request.GuestName,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _bookingRepository.AddAsync(booking);

        return (true, null, created);
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        var hasOverlap = await _bookingRepository.HasOverlapAsync(roomId, checkIn, checkOut);
        return !hasOverlap;
    }

    public Task<Booking?> GetBookingAsync(int id)
    {
        return _bookingRepository.GetByIdWithRoomAsync(id);
    }

    public Task<IEnumerable<Booking>> GetBookingsByUserAsync(string userId)
    {
        return _bookingRepository.GetByUserIdAsync(userId);
    }

    public async Task<bool> CancelBookingAsync(int id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null) return false;

        await _bookingRepository.RemoveAsync(booking);
        return true;
    }
}

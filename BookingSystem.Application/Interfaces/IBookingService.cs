using BookingSystem.Application.DTOs;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

public interface IBookingService
{
    Task<(bool Success, string? Error, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request, string userId);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut);
    Task<Booking?> GetBookingAsync(int id);
    Task<IEnumerable<Booking>> GetBookingsByUserAsync(string userId);
    Task<bool> CancelBookingAsync(int id);
}

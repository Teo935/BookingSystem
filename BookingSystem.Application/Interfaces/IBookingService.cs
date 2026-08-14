using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

public interface IBookingService
{
    Task<(bool Success, string? Error, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request, string userId);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut);
    Task<(BookingAccessResult Result, Booking? Booking)> GetBookingAsync(int id, string userId, bool isAdmin);
    Task<IEnumerable<Booking>> GetBookingsByUserAsync(string userId);
    Task<BookingAccessResult> CancelBookingAsync(int id, string userId, bool isAdmin);
}

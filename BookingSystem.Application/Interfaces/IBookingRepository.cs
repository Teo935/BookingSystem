using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

public interface IBookingRepository
{
    Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut);
    Task<Booking> AddAsync(Booking booking);
    Task<Booking?> GetByIdAsync(int id);
    Task<Booking?> GetByIdWithRoomAsync(int id);
    Task RemoveAsync(Booking booking);
}

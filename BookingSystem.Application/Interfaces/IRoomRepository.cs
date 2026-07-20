using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id);
    Task<List<Room>> GetAllAsync();
    Task<Room> AddAsync(Room room);
    Task<Room?> UpdateAsync(int id, Room updatedRoom);
    Task<bool> HasBookingsAsync(int roomId);
    Task RemoveAsync(Room room);
}

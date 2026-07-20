using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

public interface IRoomService
{
    Task<(bool Success, string? Error, Room? Room)> CreateRoomAsync(CreateRoomRequest request);
    Task<List<Room>> GetAllRoomsAsync();
    Task<Room?> GetRoomAsync(int id);
    Task<Room?> UpdateRoomAsync(int id, UpdateRoomRequest request);
    Task<RoomDeleteResult> DeleteRoomAsync(int id);
}

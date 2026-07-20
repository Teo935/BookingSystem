using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;

    public RoomService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<(bool Success, string? Error, Room? Room)> CreateRoomAsync(CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Name is required.", null);

        if (request.PricePerNight <= 0)
            return (false, "PricePerNight must be greater than zero.", null);

        var room = new Room
        {
            Name = request.Name,
            PricePerNight = request.PricePerNight
        };

        var created = await _roomRepository.AddAsync(room);

        return (true, null, created);
    }

    public Task<List<Room>> GetAllRoomsAsync()
    {
        return _roomRepository.GetAllAsync();
    }

    public Task<Room?> GetRoomAsync(int id)
    {
        return _roomRepository.GetByIdAsync(id);
    }

    public Task<Room?> UpdateRoomAsync(int id, UpdateRoomRequest request)
    {
        var updatedRoom = new Room
        {
            Name = request.Name,
            PricePerNight = request.PricePerNight
        };

        return _roomRepository.UpdateAsync(id, updatedRoom);
    }

    public async Task<RoomDeleteResult> DeleteRoomAsync(int id)
    {
        var room = await _roomRepository.GetByIdAsync(id);
        if (room == null) return RoomDeleteResult.NotFound;

        var hasBookings = await _roomRepository.HasBookingsAsync(id);
        if (hasBookings) return RoomDeleteResult.Conflict;

        await _roomRepository.RemoveAsync(room);
        return RoomDeleteResult.Success;
    }
}

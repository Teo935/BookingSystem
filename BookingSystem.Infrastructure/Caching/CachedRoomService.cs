using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using Microsoft.Extensions.Options;

namespace BookingSystem.Infrastructure.Caching;

public class CachedRoomService : IRoomService
{
    private const string AllRoomsCacheKey = "rooms:all";

    private readonly IRoomService _inner;
    private readonly ICacheService _cache;
    private readonly TimeSpan _roomsCacheDuration;

    public CachedRoomService(IRoomService inner, ICacheService cache, IOptions<CacheSettings> cacheSettings)
    {
        _inner = inner;
        _cache = cache;
        _roomsCacheDuration = TimeSpan.FromSeconds(cacheSettings.Value.RoomsCacheDurationSeconds);
    }

    public async Task<List<Room>> GetAllRoomsAsync()
    {
        var cached = await _cache.GetAsync<List<Room>>(AllRoomsCacheKey);
        if (cached != null)
            return cached;

        var rooms = await _inner.GetAllRoomsAsync();
        await _cache.SetAsync(AllRoomsCacheKey, rooms, _roomsCacheDuration);
        return rooms;
    }

    public async Task<(bool Success, string? Error, Room? Room)> CreateRoomAsync(CreateRoomRequest request)
    {
        var result = await _inner.CreateRoomAsync(request);
        if (result.Success)
            await _cache.RemoveAsync(AllRoomsCacheKey);

        return result;
    }

    public async Task<Room?> UpdateRoomAsync(int id, UpdateRoomRequest request)
    {
        var result = await _inner.UpdateRoomAsync(id, request);
        if (result != null)
            await _cache.RemoveAsync(AllRoomsCacheKey);

        return result;
    }

    public async Task<RoomDeleteResult> DeleteRoomAsync(int id)
    {
        var result = await _inner.DeleteRoomAsync(id);
        if (result == RoomDeleteResult.Success)
            await _cache.RemoveAsync(AllRoomsCacheKey);

        return result;
    }

    public Task<Room?> GetRoomAsync(int id)
    {
        return _inner.GetRoomAsync(id);
    }
}

using Microsoft.EntityFrameworkCore;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

// Repository Pattern: unico punto del progetto (insieme a BookingRepository) che parla
// con Entity Framework Core. Implementa IRoomRepository (definita in Application) —
// nessuna business logic qui dentro, solo query ed operazioni CRUD dirette.
public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _db;

    public RoomRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Room?> GetByIdAsync(int id)
    {
        return await _db.Rooms.FindAsync(id);
    }

    public async Task<List<Room>> GetAllAsync()
    {
        return await _db.Rooms.ToListAsync();
    }

    public async Task<Room> AddAsync(Room room)
    {
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        return room;
    }

    public async Task<Room?> UpdateAsync(int id, Room updatedRoom)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return null;

        room.Name = updatedRoom.Name;
        room.PricePerNight = updatedRoom.PricePerNight;

        await _db.SaveChangesAsync();

        return room;
    }

    // Query pura: risponde solo "sì/no", senza interpretare cosa significhi per la
    // cancellazione — quella decisione (bloccare con 409 Conflict) vive in RoomService.
    public async Task<bool> HasBookingsAsync(int roomId)
    {
        return await _db.Bookings.AnyAsync(b => b.RoomId == roomId);
    }

    public async Task RemoveAsync(Room room)
    {
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
    }
}

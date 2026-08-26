using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

// Repository Pattern: contratto astratto per l'accesso ai dati di Room, dichiarato qui
// nell'Application layer (che non conosce Entity Framework Core) e implementato in
// Infrastructure (RoomRepository). Questo è il punto in cui si applica il Dependency
// Inversion Principle: il Service dipende da questa interfaccia, non dal database
// concreto, e il progetto Application può essere compilato senza mai referenziare EF Core.
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id);
    Task<List<Room>> GetAllAsync();
    Task<Room> AddAsync(Room room);
    Task<Room?> UpdateAsync(int id, Room updatedRoom);

    // Query pura, senza interpretazione: risponde solo sì/no. La decisione su cosa
    // significhi "sì" per la cancellazione (blocco 409) resta nel Service.
    Task<bool> HasBookingsAsync(int roomId);
    Task RemoveAsync(Room room);
}

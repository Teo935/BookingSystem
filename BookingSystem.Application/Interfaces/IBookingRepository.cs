using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

// Repository Pattern per Booking, stesso ruolo di IRoomRepository: astrae l'accesso ai
// dati così che BookingService non dipenda mai direttamente da Entity Framework Core.
public interface IBookingRepository
{
    // Verifica se esiste già una prenotazione sulla stessa Room che si sovrappone
    // all'intervallo [checkIn, checkOut). L'implementazione (BookingRepository) usa la
    // condizione checkIn < b.CheckOut && checkOut > b.CheckIn: qualunque nuova query
    // sulle date deve restare coerente con questa stessa logica di intervallo.
    Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut);
    Task<Booking> AddAsync(Booking booking);
    Task<Booking?> GetByIdAsync(int id);
    Task<Booking?> GetByIdWithRoomAsync(int id);
    Task<IEnumerable<Booking>> GetByUserIdAsync(string userId);
    Task RemoveAsync(Booking booking);
}

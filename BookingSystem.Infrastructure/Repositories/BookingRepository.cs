using Microsoft.EntityFrameworkCore;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

// Repository Pattern per Booking: implementa IBookingRepository (Application) sopra
// Entity Framework Core. Nessuna business logic (ownership, validazione date) qui —
// resta tutta in BookingService.
public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    public BookingRepository(AppDbContext db)
    {
        _db = db;
    }

    // Query più importante del progetto: due intervalli [checkIn, checkOut) si
    // sovrappongono se e solo se "l'inizio del nuovo è prima della fine dell'esistente"
    // E "la fine del nuovo è dopo l'inizio dell'esistente" — questa è la condizione
    // standard per il confronto di intervalli di date, e ogni nuova query sulle
    // prenotazioni deve restare coerente con questa stessa logica.
    public async Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        return await _db.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            checkIn < b.CheckOut &&
            checkOut > b.CheckIn);
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await _db.Bookings.FindAsync(id);
    }

    // Include(b => b.Room) forza un eager load della Room collegata (JOIN in una sola
    // query invece di una query separata dopo): serve perché il dettaglio prenotazione
    // esposto dal Controller include i dati della stanza.
    public async Task<Booking?> GetByIdWithRoomAsync(int id)
    {
        return await _db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(string userId)
    {
        return await _db.Bookings.Include(b => b.Room).Where(b => b.UserId == userId).ToListAsync();
    }

    public async Task RemoveAsync(Booking booking)
    {
        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();
    }
}

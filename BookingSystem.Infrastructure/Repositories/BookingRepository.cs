using Microsoft.EntityFrameworkCore;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    public BookingRepository(AppDbContext db)
    {
        _db = db;
    }

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

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Identity;

namespace BookingSystem.Infrastructure.Data;

// Ponte tra il Domain (Room, Booking) e il database relazionale via Entity Framework
// Core. Eredita da IdentityDbContext<ApplicationUser> invece che da DbContext semplice:
// questo aggiunge automaticamente le tabelle di ASP.NET Identity (AspNetUsers,
// AspNetRoles, ecc.) allo stesso schema, senza doverle definire a mano.
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // decimal senza precisione esplicita userebbe il default di SQL Server
        // (decimal(18,2)) comunque, ma dichiararlo qui evita il warning EF Core "no store
        // type specified for decimal" e rende esplicito il rischio di troncamento.
        modelBuilder.Entity<Room>()
            .Property(r => r.PricePerNight)
            .HasPrecision(18, 2);

        // DeleteBehavior.Restrict: EF Core non deve cancellare in cascata le Booking se
        // la Room viene eliminata — la regola "niente cancellazione con prenotazioni
        // attive" è comunque già applicata esplicitamente in RoomService prima di questo.
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indice su RoomId: le query più frequenti (HasOverlapAsync, HasBookingsAsync)
        // filtrano sempre per RoomId, quindi qui l'indice ha un impatto reale.
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.RoomId);
    }

}

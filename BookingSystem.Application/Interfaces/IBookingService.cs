using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

// Service Layer per Booking. userId/isAdmin sono passati come primitivi (string/bool),
// non come tipi ASP.NET/ClaimsPrincipal: l'Application layer non deve conoscere
// l'infrastruttura di autenticazione, è compito del Controller estrarli dal token JWT.
public interface IBookingService
{
    Task<(bool Success, string? Error, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request, string userId);

    // Usato dall'endpoint pubblico di verifica disponibilità: nota che non controlla
    // se la Room esiste (comportamento noto, non un bug — vedi BookingService).
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut);

    // Get/Cancel applicano il controllo di ownership: BookingAccessResult distingue
    // NotFound (id inesistente) da Forbidden (esiste ma non è tua e non sei Admin).
    Task<(BookingAccessResult Result, Booking? Booking)> GetBookingAsync(int id, string userId, bool isAdmin);
    Task<IEnumerable<Booking>> GetBookingsByUserAsync(string userId);
    Task<BookingAccessResult> CancelBookingAsync(int id, string userId, bool isAdmin);
}

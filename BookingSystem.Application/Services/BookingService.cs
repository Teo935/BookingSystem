using BookingSystem.Application.Common;
using BookingSystem.Application.DTOs;
using BookingSystem.Application.Events;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Services;

// Service Layer per le prenotazioni: valida le richieste, applica le regole di
// ownership e orchestra Repository + pubblicazione eventi. Tre dipendenze, tutte
// iniettate per interfaccia (Dependency Injection) — nessun riferimento diretto a
// Entity Framework o a RabbitMQ.
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IEventPublisher _eventPublisher;

    public BookingService(IBookingRepository bookingRepository, IRoomRepository roomRepository, IEventPublisher eventPublisher)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<(bool Success, string? Error, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request, string userId)
    {
        if (request.CheckIn >= request.CheckOut)
        {
            return (false, "CheckIn date must be before CheckOut date.", null);
        }

        var room = await _roomRepository.GetByIdAsync(request.RoomId);
        if (room == null)
        {
            return (false, "Room does not exist.", null);
        }

        // Regola di business centrale del progetto: due prenotazioni sulla stessa Room
        // non possono avere intervalli di date sovrapposti. La query vera e propria vive
        // in BookingRepository.HasOverlapAsync (condizione checkIn < b.CheckOut &&
        // checkOut > b.CheckIn) — qui si controlla solo l'esito.
        var hasOverlap = await _bookingRepository.HasOverlapAsync(request.RoomId, request.CheckIn, request.CheckOut);
        if (hasOverlap)
        {
            return (false, "Room is already booked for the selected dates.", null);
        }

        var booking = new Booking
        {
            RoomId = request.RoomId,
            UserId = userId,
            GuestName = request.GuestName,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _bookingRepository.AddAsync(booking);

        // Event-Driven: l'evento viene pubblicato solo DOPO che la prenotazione è stata
        // salvata con successo. RabbitMqEventPublisher non rilancia mai eccezioni, quindi
        // se il broker è giù la prenotazione resta comunque creata — solo la notifica
        // (simulata via log dal consumer) viene persa.
        await _eventPublisher.PublishAsync(new BookingCreatedEvent(
            created.Id, created.RoomId, room.Name, created.UserId, created.GuestName,
            created.CheckIn, created.CheckOut, DateTime.UtcNow));

        return (true, null, created);
    }

    // Nota: non verifica che la Room esista, solo che non ci sia sovrapposizione di date
    // — comportamento noto e voluto, non un bug (CreateBookingAsync fa comunque il
    // controllo completo al momento della prenotazione vera).
    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        var hasOverlap = await _bookingRepository.HasOverlapAsync(roomId, checkIn, checkOut);
        return !hasOverlap;
    }

    // Controllo di ownership: solo chi ha creato la prenotazione o un utente con ruolo
    // Admin può leggerla. L'esistenza è verificata PRIMA dell'ownership, così un id
    // inesistente ritorna sempre NotFound (404) anche per chi non è owner.
    public async Task<(BookingAccessResult Result, Booking? Booking)> GetBookingAsync(int id, string userId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdWithRoomAsync(id);
        if (booking == null) return (BookingAccessResult.NotFound, null);

        if (booking.UserId != userId && !isAdmin) return (BookingAccessResult.Forbidden, null);

        return (BookingAccessResult.Success, booking);
    }

    public Task<IEnumerable<Booking>> GetBookingsByUserAsync(string userId)
    {
        return _bookingRepository.GetByUserIdAsync(userId);
    }

    // Stessa logica di ownership di GetBookingAsync, applicata alla cancellazione.
    public async Task<BookingAccessResult> CancelBookingAsync(int id, string userId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null) return BookingAccessResult.NotFound;

        if (booking.UserId != userId && !isAdmin) return BookingAccessResult.Forbidden;

        await _bookingRepository.RemoveAsync(booking);

        await _eventPublisher.PublishAsync(new BookingCancelledEvent(
            booking.Id, booking.RoomId, booking.UserId, booking.GuestName,
            booking.CheckIn, booking.CheckOut, DateTime.UtcNow));

        return BookingAccessResult.Success;
    }
}

using BookingSystem.Application.Events;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Infrastructure.Messaging;

// Logica pura del consumer, deliberatamente isolata da RabbitMQ.Client (nessun
// riferimento alla libreria qui dentro) così da restare facilmente testabile: riceve
// già l'evento deserializzato e decide solo "cosa fare" con esso. In un sistema reale
// qui partirebbe l'invio di un'email vera; qui si limita a loggare, a scopo didattico.
public class BookingNotificationHandler
{
    private readonly ILogger<BookingNotificationHandler> _logger;

    public BookingNotificationHandler(ILogger<BookingNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleBookingCreatedAsync(BookingCreatedEvent evt)
    {
        _logger.LogInformation(
            "[Notifica] Simulazione invio email di conferma a {GuestName} per la prenotazione #{BookingId} ({RoomName}, {CheckIn:d} - {CheckOut:d}).",
            evt.GuestName, evt.BookingId, evt.RoomName, evt.CheckIn, evt.CheckOut);

        return Task.CompletedTask;
    }

    public Task HandleBookingCancelledAsync(BookingCancelledEvent evt)
    {
        _logger.LogInformation(
            "[Notifica] Simulazione invio email di cancellazione a {GuestName} per la prenotazione #{BookingId} ({CheckIn:d} - {CheckOut:d}).",
            evt.GuestName, evt.BookingId, evt.CheckIn, evt.CheckOut);

        return Task.CompletedTask;
    }
}

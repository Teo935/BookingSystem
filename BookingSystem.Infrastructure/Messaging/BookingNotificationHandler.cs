using BookingSystem.Application.Events;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Infrastructure.Messaging;

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

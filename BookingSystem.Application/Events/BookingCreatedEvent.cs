namespace BookingSystem.Application.Events;

// Evento di dominio pubblicato su RabbitMQ dopo che una prenotazione è stata salvata con
// successo (routing key "booking.created" sull'exchange topic "booking.events"). E' un
// record immutabile: descrive un fatto già accaduto, non un comando. Il consumer
// (BookingNotificationHandler) lo usa per simulare l'invio di un'email di conferma.
public record BookingCreatedEvent(
    int BookingId,
    int RoomId,
    string RoomName,
    string? UserId,
    string GuestName,
    DateTime CheckIn,
    DateTime CheckOut,
    DateTime OccurredAtUtc);

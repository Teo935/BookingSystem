namespace BookingSystem.Application.Events;

// Simmetrico a BookingCreatedEvent: pubblicato dopo una cancellazione riuscita (routing
// key "booking.created" e "booking.cancelled" condividono lo stesso exchange e la stessa
// coda "booking.notifications" — la routing key distingue quale email simulare).
public record BookingCancelledEvent(
    int BookingId,
    int RoomId,
    string? UserId,
    string GuestName,
    DateTime CheckIn,
    DateTime CheckOut,
    DateTime OccurredAtUtc);

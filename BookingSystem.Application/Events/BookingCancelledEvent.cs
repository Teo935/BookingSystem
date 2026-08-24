namespace BookingSystem.Application.Events;

public record BookingCancelledEvent(
    int BookingId,
    int RoomId,
    string? UserId,
    string GuestName,
    DateTime CheckIn,
    DateTime CheckOut,
    DateTime OccurredAtUtc);

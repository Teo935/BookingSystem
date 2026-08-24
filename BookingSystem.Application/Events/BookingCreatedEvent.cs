namespace BookingSystem.Application.Events;

public record BookingCreatedEvent(
    int BookingId,
    int RoomId,
    string RoomName,
    string? UserId,
    string GuestName,
    DateTime CheckIn,
    DateTime CheckOut,
    DateTime OccurredAtUtc);

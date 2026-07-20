namespace BookingSystem.Application.DTOs;

public class CreateBookingRequest
{
    public int RoomId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
}

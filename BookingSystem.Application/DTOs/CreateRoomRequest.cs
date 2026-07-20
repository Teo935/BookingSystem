namespace BookingSystem.Application.DTOs;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
}

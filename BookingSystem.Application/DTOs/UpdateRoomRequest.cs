namespace BookingSystem.Application.DTOs;

public class UpdateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
}

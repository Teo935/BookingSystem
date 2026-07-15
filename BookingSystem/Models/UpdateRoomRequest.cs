namespace BookingSystem.Models;

public class UpdateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
}
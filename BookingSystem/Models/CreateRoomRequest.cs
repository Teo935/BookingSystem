namespace BookingSystem.Models;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
}
namespace BookingSystem.Domain.Entities;

// Domain layer: entità pura, senza alcuna dipendenza esterna (niente EF Core, niente
// attributi di validazione, niente riferimenti agli altri progetti). E' il livello più
// interno della Clean Architecture: tutti gli altri layer dipendono da questo, mai il contrario.
public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
}

namespace BookingSystem.Domain.Entities;

// Entità pura del Domain layer: rappresenta una prenotazione di una Room in un
// intervallo di date. Nessuna business logic qui dentro (es. il controllo di
// sovrapposizione date vive nel Repository/Service) — questa classe descrive solo
// la forma dei dati.
public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    // Proprietario della prenotazione (Id dell'utente Identity). E' una semplice stringa
    // e non una foreign key/navigation property verso ApplicationUser: se ci fosse una
    // relazione EF Core vera, il Domain dovrebbe conoscere i tipi di ASP.NET Identity,
    // violando la regola "i layer interni non dipendono da quelli esterni".
    public string? UserId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace BookingSystem.Application.Common;

// Un semplice bool/null non basterebbe: il Controller deve distinguere "stanza non
// trovata" (404) da "stanza con prenotazioni attive, cancellazione bloccata" (409
// Conflict). Questo enum porta quella distinzione dal Service fino al Controller
// senza che il Service debba lanciare eccezioni per un caso di business normale.
public enum RoomDeleteResult
{
    Success,
    NotFound,
    Conflict
}

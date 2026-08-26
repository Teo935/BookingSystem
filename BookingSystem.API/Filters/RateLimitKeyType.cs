namespace BookingSystem.API.Filters;

// IpAddress per endpoint anonimi (Login/Register: non c'è ancora un utente
// autenticato). UserId per endpoint protetti (es. CreateBooking) dove l'IP non è
// affidabile come chiave — più utenti dietro lo stesso NAT/proxy condividerebbero
// lo stesso limite.
public enum RateLimitKeyType
{
    IpAddress,
    UserId
}

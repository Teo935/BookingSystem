namespace BookingSystem.Application.Common;

// Usato da GetBookingAsync/CancelBookingAsync per portare al Controller l'esito di un
// controllo di ownership (solo il proprietario della prenotazione o un Admin possono
// leggerla/cancellarla). L'esistenza viene verificata prima dell'autorizzazione: un id
// inesistente ritorna sempre NotFound (404), anche per chi non è owner — mai Forbidden
// prima di sapere se la risorsa esiste davvero.
public enum BookingAccessResult
{
    Success,
    NotFound,
    Forbidden
}

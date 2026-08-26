namespace BookingSystem.Application.Common;

// Costanti dei nomi di ruolo ASP.NET Identity, in un unico punto. Restano stringhe
// hardcoded di proposito (sono richieste anche a compile-time da attributi come
// [Authorize(Roles = Roles.Admin)], che non accettano espressioni dinamiche) — questa
// classe serve solo a evitare di ripetere le stesse stringhe in più punti del codice.
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

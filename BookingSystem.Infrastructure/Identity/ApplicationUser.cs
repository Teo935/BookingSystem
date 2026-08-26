using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Identity;

// Estende IdentityUser (classe base di ASP.NET Identity, con Email/PasswordHash/ecc.
// già pronti) senza aggiungere proprietà: il progetto non ha bisogno di altri dati
// utente oltre a quelli standard. Vive in Infrastructure, non in Domain, perché è
// legata a un dettaglio implementativo (ASP.NET Identity), non a un concetto di business.
public class ApplicationUser : IdentityUser
{
}

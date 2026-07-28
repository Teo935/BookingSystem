# JWT e ASP.NET Core Identity nel progetto — spiegazione

> Scritto il 2026-07-28 su richiesta, per poter rileggere senza dover
> richiedere di nuovo la spiegazione. Riferimento: sessione del 2026-07-21
> "Integrazione ASP.NET Core Identity, JWT Authentication e Role-Based
> Authorization" in `context/sessions.md`.

## 1. ASP.NET Core Identity — cos'è e come lo abbiamo usato

**Identity** è il sistema pronto-all'uso di Microsoft per gestire utenti,
password (con hashing sicuro automatico) e ruoli, senza doverlo scrivere a
mano. Fornisce classi come:

- `IdentityUser` — la classe base che rappresenta un utente (Id, Email,
  PasswordHash, ecc.)
- `IdentityRole` — rappresenta un ruolo (es. "Admin", "User")
- `UserManager<T>` — il servizio per creare utenti, verificare password,
  assegnare ruoli
- `RoleManager<T>` — il servizio per creare/gestire i ruoli

Nel progetto:

- **`ApplicationUser : IdentityUser`**
  (`BookingSystem.Infrastructure/Identity/ApplicationUser.cs`) — la nostra
  classe utente. È vuota perché non abbiamo bisogno di campi in più oltre
  a quelli standard di Identity.
- **`AppDbContext : IdentityDbContext<ApplicationUser>`** — invece di
  ereditare solo da `DbContext`, ora eredita da `IdentityDbContext`, che
  aggiunge automaticamente le tabelle `AspNetUsers`, `AspNetRoles`,
  `AspNetUserRoles` ecc. al database (create dalla migration
  `AddIdentityAndBookingUserId`).
- In `BookingSystem.API/Program.cs` (righe 44-46):

  ```csharp
  builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
      .AddEntityFrameworkStores<AppDbContext>()
      .AddDefaultTokenProviders();
  ```

  Questo registra `UserManager`/`RoleManager` nel sistema di Dependency
  Injection (DI — il meccanismo con cui ASP.NET Core fornisce
  automaticamente le dipendenze ai costruttori delle classi) e dice loro
  di salvare/leggere i dati tramite `AppDbContext` (quindi su SQL Server).

## 2. I Ruoli (Admin e User)

- All'avvio, `IdentitySeeder.SeedAsync`
  (`BookingSystem.Infrastructure/Identity/IdentitySeeder.cs`) crea i ruoli
  `Admin` e `User` se non esistono (`RoleManager.CreateAsync`) e crea
  l'utente admin di default (`admin@bookingsystem.com`) se non esiste,
  assegnandogli il ruolo `Admin`.
- Viene chiamato in `Program.cs` (righe 82-86), dentro uno `scope` creato
  subito dopo `builder.Build()` — necessario perché `RoleManager`/
  `UserManager` sono servizi "scoped" (vivono per una singola richiesta) e
  fuori da una richiesta HTTP vera serve creare uno scope manuale per
  poterli usare.
- Ogni nuovo utente registrato tramite `AuthService.RegisterAsync` riceve
  automaticamente il ruolo `User` (`DefaultRole`).
- Sui Controller, `[Authorize(Roles = "Admin")]` dice al framework:
  "lascia passare solo se il token JWT della richiesta contiene un claim
  di ruolo (`ClaimTypes.Role`) con valore Admin". Questo controllo è
  automatico, fornito da ASP.NET Core — non abbiamo scritto codice per
  verificarlo a mano.

## 3. Il Token JWT — come viene generato e validato

Un **JWT** (JSON Web Token) è una stringa firmata digitalmente che
contiene delle informazioni ("claims") sull'utente — in questo caso Id,
Email e Ruoli. È firmato, quindi il server può verificare che non sia
stato alterato, ma **non è cifrato**: chiunque può leggerne il contenuto
(non ci va mai una password).

**Generazione** — `JwtTokenGenerator.GenerateToken`
(`BookingSystem.Infrastructure/Identity/JwtTokenGenerator.cs`, riga 17):

1. Crea una lista di `Claim` (coppie chiave/valore): `NameIdentifier`
   (l'Id utente), `Email`, e un claim `Role` per ogni ruolo dell'utente.
2. Crea una chiave simmetrica (`SymmetricSecurityKey`) a partire dalla
   `SecretKey` in configurazione — la stessa chiave serve sia per firmare
   che per verificare, per questo va tenuta segreta.
3. Firma il token con l'algoritmo HMAC-SHA256.
4. Imposta una scadenza (`ExpirationMinutes` da configurazione).

Chi lo chiama: `AuthService.BuildAuthResponse`
(`BookingSystem.Infrastructure/Identity/AuthService.cs`, righe 60-72), sia
dopo la registrazione che dopo il login riuscito.

**Validazione** — ad ogni richiesta HTTP, il middleware configurato in
`Program.cs` (righe 53-70, `AddAuthentication(...).AddJwtBearer(...)`)
legge l'header `Authorization: Bearer <token>`, verifica firma, issuer,
audience e scadenza usando gli stessi `TokenValidationParameters`. Se
tutto è valido, ricostruisce i claims (incluso il ruolo) e li rende
disponibili al Controller — è così che `[Authorize]` e
`[Authorize(Roles = "Admin")]` fanno il loro lavoro senza codice esplicito
nostro.

`app.UseAuthentication()` (aggiunto, mancava prima) attiva questo
controllo; `app.UseAuthorization()` (già presente) applica poi gli
attributi `[Authorize]`.

## 4. Perché questi file sono in `Infrastructure/Identity` e non in `Application`

Questo è il punto chiave della Clean Architecture qui: **`Application` non
deve conoscere dettagli tecnici esterni**, solo interfacce/contratti e
logica di business pura.

- `ApplicationUser` eredita da `IdentityUser`, una classe della libreria
  `Microsoft.AspNetCore.Identity`. Se stesse in `Application`, quel
  progetto dipenderebbe da un framework di infrastruttura esterno —
  esattamente come `Domain` non deve dipendere da Entity Framework.
- `JwtTokenGenerator` usa librerie molto tecniche
  (`System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`)
  per costruire/firmare la stringa del token: è un dettaglio
  implementativo di "come genero un JWT", non una regola di business.
- `AuthService` usa `UserManager<ApplicationUser>` e (indirettamente
  tramite il seeder) `RoleManager<IdentityRole>` — servizi concreti
  forniti da Identity, che a loro volta parlano con `AppDbContext` (EF
  Core). Stesso identico motivo per cui `RoomRepository`/
  `BookingRepository` stanno in Infrastructure e non in Application.
- `IdentitySeeder` fa lo stesso: usa direttamente `RoleManager`/
  `UserManager`, quindi è infrastruttura.
- `JwtSettings` è solo un contenitore di configurazione (POCO — Plain Old
  CLR Object, cioè una classe senza logica), ma è stato messo vicino a chi
  lo consuma (`JwtTokenGenerator`) piuttosto che in un progetto a parte,
  per coerenza con gli altri file di questa area.

Quello che **è** rimasto in `Application` è il contratto:
**`IAuthService`** (`BookingSystem.Application/Interfaces/IAuthService.cs`)
e i DTO (`RegisterRequest`, `LoginRequest`, `AuthResponse`). Il Controller
(`AuthController`, in API) dipende solo da `IAuthService` — non sa nulla di
Identity, JWT o `UserManager`. È lo stesso identico pattern già usato per
`IRoomRepository`/`RoomRepository` e `IBookingRepository`/
`BookingRepository`: **l'interfaccia (il "cosa") vive nel livello interno
che la usa, l'implementazione concreta (il "come", legata a librerie
esterne) vive in Infrastructure**. Questo è il principio di Dependency
Inversion (l'ultima delle 5 regole SOLID): i livelli interni dipendono da
astrazioni, mai da dettagli tecnici concreti.

Il collegamento: `Program.cs`, riga 74 —
`builder.Services.AddScoped<IAuthService, AuthService>();` — dice al
contenitore di Dependency Injection "quando qualcuno chiede un
`IAuthService`, dagli un `AuthService`". Questa è l'unica riga in tutto il
progetto che "conosce" sia l'interfaccia che l'implementazione concreta.

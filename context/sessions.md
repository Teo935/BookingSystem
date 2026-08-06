# Log delle sessioni di lavoro

## 2026-07-20 — Analisi architetturale e refactoring verso Clean Architecture

Punto di partenza: un solo progetto ASP.NET Core (`BookingSystem.csproj`) con Controller che parlavano direttamente al database (Entity Framework Core, l'ORM — cioè la libreria che traduce oggetti C# in query SQL — usato in questo progetto), nessuna interfaccia, nessun progetto di test.

### 1. Analisi architetturale iniziale (nessuna modifica al codice)

Revisione completa del progetto per individuare violazioni dei principi SOLID (le 5 regole base di buon design orientato agli oggetti: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion) e della Clean Architecture (uno stile di progettazione che separa il codice in "livelli" concentrici, dove i livelli più interni — es. le regole di business — non devono mai dipendere da quelli più esterni, es. il database).

Problemi principali trovati:
- Nessuna separazione in livelli: i Controller usavano `AppDbContext` (la classe di EF Core che rappresenta la connessione al database) direttamente.
- Validazione e regole di business scritte dentro i Controller.
- La regola "una stanza non può avere due prenotazioni sovrapposte" era duplicata in due punti diversi.
- Nessuna interfaccia in tutto il progetto → impossibile scrivere test automatici in isolamento.
- Cancellazione di una stanza gestita catturando un'eccezione generica del database invece di controllare esplicitamente la condizione di business.

Report completo con gravità/motivazione/soluzione per ogni problema + roadmap di refactoring prioritizzata.

### 2. Refactoring del Controller Stanze (`RoomsController`)

Creato `IRoomService`/`RoomService` (un'interfaccia + la sua implementazione): la validazione e la logica di persistenza sono state spostate fuori dal controller, che ora si limita a ricevere la richiesta HTTP e chiamare il service.

### 3. Refactoring del layer Service

Estratto `IRoomRepository`/`RoomRepository`: il Repository è il livello che parla col database; il Service non conosce più direttamente Entity Framework, dipende solo dall'interfaccia del Repository. Applicato lo stesso trattamento a `BookingService`, introducendo `IBookingRepository`/`BookingRepository`.

### 4. Refactoring del layer Repository

Trovata business logic infiltrata in `RoomRepository`: il metodo di cancellazione interpretava un'eccezione tecnica di EF Core (`DbUpdateException`) come "la stanza ha prenotazioni collegate". Spostata questa decisione nel Service, lasciando al Repository solo query pure (es. `HasBookingsAsync`, che si limita a rispondere sì/no senza interpretare nulla).

### 5. Completamento layer per le Prenotazioni

`BookingsController` bypassava ancora il Service per 3 azioni su 4 (disponibilità, dettaglio, cancellazione), accedendo al database direttamente. Creato `IBookingService`/estensione di `IBookingRepository` per portare anche queste operazioni dietro le interfacce, eliminando ogni riferimento a Entity Framework dal Controller.

### 6. Riorganizzazione in 4 progetti separati (vera Clean Architecture)

Il progetto singolo è stato smontato in **4 progetti .NET distinti**, ognuno con le sue responsabilità e la sua "csproj" (il file che definisce un progetto .NET):

- **BookingSystem.Domain** — le entità pure (`Booking`, `Room`), zero dipendenze esterne.
- **BookingSystem.Application** — DTO (Data Transfer Object, gli oggetti usati per ricevere le richieste HTTP), interfacce (i "contratti" che Repository e Service devono rispettare) e Service (dove vive la logica di business). Dipende solo da Domain.
- **BookingSystem.Infrastructure** — `AppDbContext`, le Migrations (i file generati da EF Core che descrivono come cambia lo schema del database nel tempo) e le implementazioni concrete dei Repository. Dipende da Domain e Application.
- **BookingSystem.API** — Controller, `Program.cs` (il punto di avvio dell'applicazione, dove si configurano le dipendenze) e file di configurazione. Dipende da Application e Infrastructure.

Le dipendenze puntano tutte verso l'interno (API → Application/Infrastructure → Application → Domain), mai il contrario — questo è il cuore della Clean Architecture.

Il vecchio progetto `BookingSystem/` è stato rimosso dopo aver spostato tutto il contenuto nei 4 nuovi progetti (namespace aggiornati ovunque, incluse le Migrations che referenziano il nome completo delle entità). Il database SQLite di sviluppo esistente è stato copiato nel nuovo progetto API per non perdere i dati di test già presenti.

Build della solution completa verificata: 0 errori, 0 warning.

### 7. Revisione finale

Controllo di chiusura su tutto il progetto: dipendenze tra livelli corrette e senza cicli, nessuna business logic residua in Controller o Repository, nessun codice duplicato, nomenclatura coerente (prefisso `I` per le interfacce, suffisso `Request` per i DTO), organizzazione delle cartelle coerente con i namespace.

**Unico punto ancora aperto**: non esiste ancora un progetto di test automatici (`BookingSystem.Tests`). La nuova struttura lo rende finalmente possibile a basso costo, dato che ogni Service e Repository è ora dietro un'interfaccia (quindi "mockabile", cioè sostituibile con una versione finta nei test) — ma non è stato ancora creato.

### Vincoli rispettati in tutte le fasi
Nessun endpoint pubblico modificato, nessun comportamento funzionale alterato, nessuna introduzione di CQRS, MediatR o Domain-Driven Design avanzato (pattern più complessi che non erano necessari per le dimensioni di questo progetto).

## 2026-07-21 — Creazione del progetto di test automatici

Punto di partenza: chiuso l'unico punto aperto della sessione precedente, cioè l'assenza di un progetto di test nella solution.

### 1. Nuovo progetto `BookingSystem.Tests`

Creato con il template xUnit (framework di test standard per .NET), forzando `net8.0` come target (lo SDK di default sulla macchina era .NET 9, disallineato con gli altri 4 progetti). Aggiunto a `BookingSystem.sln`, con riferimento (`ProjectReference`) solo verso `BookingSystem.Application` — sufficiente perché i Service dipendono unicamente dalle interfacce Repository (`IRoomRepository`, `IBookingRepository`), mai da Entity Framework Core direttamente, quindi non serve alcun riferimento a `BookingSystem.Infrastructure` né un database reale/in-memory nei test.

Aggiunto il package **Moq** (libreria per creare oggetti "finti"/mock che simulano un'interfaccia, così da isolare la classe sotto test dalle sue dipendenze reali) alla dotazione già inclusa dal template (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`).

Verificato che `azure-pipelines.yml` (che usa `VSTest@2`, non `dotnet test`) scopre automaticamente l'assembly di test grazie al pattern di default basato sul nome contenente "test" — nessuna modifica alla pipeline necessaria.

### 2. Test scritti

`RoomServiceTests.cs` (13 test) e `BookingServiceTests.cs` (12 test), namespace `BookingSystem.Tests.Services`, con mock di `IRoomRepository`/`IBookingRepository` iniettati nei Service reali (`RoomService`, `BookingService`). Copertura di tutti i branch di business logic:

- `RoomService`: validazione `CreateRoomAsync` (nome vuoto/whitespace, prezzo zero/negativo, caso valido), pass-through di `GetAllRoomsAsync`/`GetRoomAsync`/`UpdateRoomAsync`, i 3 esiti di `DeleteRoomAsync` (`NotFound`/`Conflict`/`Success` dell'enum `RoomDeleteResult`).
- `BookingService`: validazione date (`CheckIn >= CheckOut`), camera inesistente, sovrapposizione date, caso valido con verifica di `CreatedAt`; `IsRoomAvailableAsync` (incluso un test che documenta esplicitamente che il metodo non controlla l'esistenza della camera — comportamento noto, non "corretto"); `GetBookingAsync`; i 2 esiti di `CancelBookingAsync`.

Stile: naming `Metodo_Scenario_RisultatoAtteso`, struttura Arrange/Act/Assert con commenti, `Verify` usato solo dove è rilevante affermare che un metodo NON è stato chiamato (es. `RemoveAsync` non deve scattare se la entità non esiste).

### 3. Verifica

`dotnet build` sull'intera solution: 0 errori, 0 warning. `dotnet test`: **25/25 test superati**.

### Vincoli rispettati
Nessuna modifica al codice di produzione (Service/Repository/Controller), nessuna modifica alla pipeline CI, solo framework di test standard (xUnit + Moq) senza librerie esotiche.

## 2026-07-21 — Migrazione da SQLite a SQL Server

Punto di partenza: il database di sviluppo era SQLite (`bookingsystem.db`), con provider EF Core (Entity Framework Core, l'ORM del progetto) `Microsoft.EntityFrameworkCore.Sqlite`.

### Modifiche

- `BookingSystem.Infrastructure.csproj`: sostituito il pacchetto NuGet `Microsoft.EntityFrameworkCore.Sqlite` con `Microsoft.EntityFrameworkCore.SqlServer`.
- `Program.cs`: `options.UseSqlite(...)` → `options.UseSqlServer(...)`.
- `appsettings.json`: connection string aggiornata per puntare a un'istanza locale SQL Server Express (`Server=.\SQLEXPRESS;Database=BookingSystemDb;...`), già presente e in esecuzione sulla macchina.
- Rimosse le vecchie Migrations (erano scritte con tipi/annotazioni specifiche di SQLite, es. `Sqlite:Autoincrement`) e rigenerata una `InitialCreate` pulita per SQL Server.
- Rimosso il file `bookingsystem.db` ormai orfano (non tracciato in git).
- `AppDbContext`: aggiunta `HasPrecision(18, 2)` sulla proprietà `PricePerNight` di `Room`, per evitare il warning EF Core "no store type specified for decimal" che SQLite non generava (perché salvava i decimal come TEXT) ma SQL Server sì (default `decimal(18,2)`, rischio di troncamento silenzioso senza precisione esplicita).
- Aggiunto anche a `BookingSystem.API.csproj` il pacchetto `Microsoft.EntityFrameworkCore.Design`: era referenziato solo in Infrastructure con `PrivateAssets="all"`, che ne impediva la propagazione al progetto di avvio (API) — necessario perché gli strumenti EF Core (`dotnet ef`) funzionino con `--startup-project BookingSystem.API`.
- Installato il tool globale `dotnet-ef` (non era presente sulla macchina), versione allineata a EF Core 8.0.11 usata dal progetto.

### Verifica
`dotnet build`: 0 errori/warning. Migration applicata su SQL Server con successo. App avviata e testata con richieste HTTP reali (creazione, lettura, cancellazione di una Room) per confermare che il flusso end-to-end funzioni contro SQL Server.

### Vincoli rispettati
Nessuna modifica a Controller/Service/Repository/Domain, nessun cambiamento di comportamento delle API pubbliche.

## 2026-07-21 — Integrazione ASP.NET Core Identity, JWT Authentication e Role-Based Authorization

Punto di partenza: nessun meccanismo di autenticazione. Tutti gli endpoint erano pubblici, `Program.cs` non registrava né `AddAuthentication` né `UseAuthentication`.

### Decisioni architetturali (concordate con l'utente prima di implementare)

- **"Le mie prenotazioni"**: creato un nuovo endpoint dedicato `GET /api/bookings/mine` invece di filtrare l'endpoint esistente `GetBooking(id)` per proprietario. Questo ha richiesto di aggiungere un campo `UserId` (stringa nullable, senza foreign key reale né navigation property verso `ApplicationUser`, per non far dipendere `BookingSystem.Domain` da tipi di Identity) all'entità `Booking`.
- **Endpoint ambigui**: `GET /api/bookings/{id}` e `DELETE /api/bookings/{id}` sono diventati `[Authorize]`; `GET /api/rooms/{roomId}/availability` è rimasto pubblico.
- **Conferma email**: non richiesta (`RequireConfirmedEmail` di default, cioè `false`) — scelta adatta a un progetto didattico senza servizio di invio email configurato.

### Struttura per livello (Clean Architecture)

- **Domain**: solo l'aggiunta di `Booking.UserId`, nessuna dipendenza da Identity.
- **Application**: nuovi DTO (`RegisterRequest`, `LoginRequest`, `AuthResponse`) e interfaccia `IAuthService`; firme aggiornate di `IBookingService`/`IBookingRepository` per portare `UserId` (`CreateBookingAsync` accetta ora anche `userId`, nuovo `GetBookingsByUserAsync`).
- **Infrastructure** (cartella `Identity/`): `ApplicationUser : IdentityUser` (nessuna proprietà aggiuntiva), `AppDbContext` ora eredita da `IdentityDbContext<ApplicationUser>`, `JwtSettings` (POCO per la configurazione), `JwtTokenGenerator` (genera il token con claim UserId/Email/Role, firmato HMAC-SHA256), `AuthService` (usa `UserManager`/`RoleManager` di Identity per Register/Login), `IdentitySeeder` (crea i ruoli `Admin`/`User` e l'utente admin iniziale se non esistono).
- **API**: nuovo `AuthController` (`POST /api/auth/register`, `POST /api/auth/login`), `Program.cs` configura `AddIdentity`, `AddAuthentication().AddJwtBearer(...)`, `AddAuthorization`, `UseAuthentication()` (mancante prima, aggiunto prima di `UseAuthorization()`), seeding dei ruoli/admin all'avvio, Swagger con supporto al pulsante "Authorize" per il Bearer token.

### Autorizzazione applicata

- `RoomsController`: `[Authorize(Roles = "Admin")]` su Create/Update/Delete; le GET restano pubbliche.
- `BookingsController`: `[Authorize]` a livello di controller (protegge Create/Get/Cancel/mine), `[AllowAnonymous]` esplicito solo su `CheckAvailability`.

### Configurazione

`appsettings.json`, sezione `Jwt`: `SecretKey` (generata casualmente per lo sviluppo locale), `Issuer`, `Audience`, `ExpirationMinutes`. Credenziali admin seed: `admin@bookingsystem.com` / `Admin123!`.

### Migration
`dotnet ef migrations add AddIdentityAndBookingUserId` (tabelle `AspNetUsers`/`AspNetRoles`/ecc. + colonna `Bookings.UserId`), applicata con `dotnet ef database update`.

### Verifica end-to-end
`dotnet build`: 0 errori/warning. `dotnet test`: 25/25 (aggiornati i test esistenti di `BookingServiceTests` per la nuova firma di `CreateBookingAsync`, aggiunta un'asserzione sul valore di `UserId`). App avviata e testata via `curl`: login admin → JWT con ruolo Admin; registrazione nuovo utente → JWT con ruolo User; `POST /api/rooms` senza token → 401, con token User → 403, con token Admin → 201; `POST /api/bookings` con token User → 200 con `UserId` valorizzato; `GET /api/bookings/mine` → solo le prenotazioni dell'utente; `GET /api/rooms/{id}/availability` senza token → 200 (pubblico come da requisito).

### Vincoli rispettati
Nessuna modifica alla logica di business esistente di Room/Booking (solo l'aggiunta additiva di `UserId`), nessun CQRS/MediatR/microservizi introdotti, nessuna proprietà superflua su `ApplicationUser`.

## 2026-07-28 — Containerizzazione con Docker (Dockerfile + docker-compose)

Punto di partenza: l'utente aveva già creato in autonomia `BookingSystem.API/Dockerfile` (build multi-stage: SDK .NET 8 per compilare/pubblicare, runtime ASP.NET 8 per l'esecuzione) e `docker-compose.yml` (due servizi: `sqlserver` con l'immagine ufficiale `mcr.microsoft.com/mssql/server:2022-latest`, e `api` che builda dal Dockerfile), ma l'avvio falliva con l'errore `An error occurred using the connection to database 'BookingSystemDb' on server '.\SQLEXPRESS'`.

### 1. Diagnosi del primo errore (connection string sbagliata)
Causa: l'utente lanciava il container con `docker run` diretto invece che con `docker compose up`. Così facendo la variabile d'ambiente `ConnectionStrings__DefaultConnection` definita nel compose (che punta al servizio `sqlserver` con autenticazione SQL `sa`) non veniva applicata, e l'app ripiegava sulla connection string di fallback in `appsettings.json`, che puntava a `.\SQLEXPRESS` (istanza SQL Server Express installata su Windows, host) — irraggiungibile e concettualmente incompatibile da dentro un container Linux (niente autenticazione integrata di Windows, nessuna istanza SQLEXPRESS presente).

### 2. Secondo errore dopo il passaggio a `docker compose up --build` (Swagger irraggiungibile)
Log del container (`docker compose logs api`) hanno mostrato l'errore reale: `Error Number 4060 — Cannot open database "BookingSystemDb" requested by the login. Login failed for user 'sa'`. Causa: il database nel container `sqlserver` partiva completamente vuoto (le Migration erano state applicate in passato solo contro l'istanza SQLEXPRESS locale, mai contro questo nuovo container). `IdentitySeeder.SeedAsync` in `Program.cs`, eseguito all'avvio prima che Kestrel iniziasse ad ascoltare, provava a leggere la tabella `AspNetRoles` inesistente → eccezione non gestita → processo terminato prima di aprire la porta 8080.

### 3. Fix applicati in `Program.cs`
- Aggiunto `sqlOptions.EnableRetryOnFailure()` alla configurazione di `UseSqlServer`: rende l'app resiliente nel caso in cui il container SQL Server non sia ancora pronto ad accettare connessioni al primo avvio (`depends_on` in docker-compose garantisce solo l'ordine di *avvio* dei container, non la effettiva disponibilità del servizio).
- Aggiunta la chiamata `await dbContext.Database.MigrateAsync();` nello scope di avvio, prima del seeding di ruoli/utenti: il container ora crea/aggiorna automaticamente lo schema del database ad ogni avvio, senza bisogno di eseguire manualmente `dotnet ef database update` contro il container.

### 4. Verifica
Rebuild (`docker compose up --build -d`): log confermano migration applicate, ruoli e utente admin seedati con successo, `Now listening on: http://[::]:8080`. `curl http://localhost:5068/swagger/index.html` → 200.

### Nota per l'utente: differenze rispetto a un ambiente aziendale reale
Il flusso (Dockerfile multi-stage + docker-compose per orchestrare app e database in locale) è realistico, ma un ambiente aziendale di produzione aggiungerebbe: gestione dei segreti (qui password SA e JWT secret sono in chiaro nel `docker-compose.yml`, invece che in un vault o variabili non committate), un registry di immagini con pipeline CI/CD che builda/pusha l'immagine, orchestrazione a più repliche (Kubernetes/ECS invece di un singolo container docker-compose), e le migration applicate in uno step di release separato invece che ad ogni avvio dell'app (per evitare race condition tra più istanze che migrano in parallelo quando si scala orizzontalmente).

### Vincoli rispettati
Nessuna modifica a Controller/Service/Repository/Domain; le uniche modifiche di codice sono in `Program.cs` (bootstrap dell'infrastruttura, coerente con dove già viveva il seeding), nessun cambiamento di comportamento delle API pubbliche.

## 2026-07-28 — Modernizzazione di `azure-pipelines.yml`

Punto di partenza: la pipeline era ancora il template generico creato all'inizio del progetto, pensato per **.NET Framework classico** (`VSBuild@1` con pacchetto per IIS, `VSTest@2`, pool `windows-latest`) — mai allineato al progetto attuale (.NET 8, 4 progetti + `BookingSystem.Tests` con xUnit).

### Modifiche
- Sostituito `NuGetToolInstaller@1` + `NuGetCommand@2` + `VSBuild@1` con i task `dotnet` CLI equivalenti: `UseDotNet@2` (installa SDK .NET 8), poi `DotNetCoreCLI@2` per i tre step `restore` → `build` (`--configuration Release --no-restore`) → `test` (mirato a `**/BookingSystem.Tests.csproj`, `--no-build`).
- Rimosso `VSTest@2`: prima scopriva l'assembly di test solo per convenzione di nome, ora il test è uno step esplicito della pipeline.
- Cambiato il pool da `windows-latest` a `ubuntu-latest`: coerente con un progetto .NET 8 cross-platform destinato a girare in container Linux (vedi sessione Docker sopra), elimina la dipendenza implicita da IIS/Windows del vecchio template.

### Verifica
Riprodotta in locale la stessa sequenza della pipeline: `dotnet restore BookingSystem.sln` → `dotnet build --configuration Release --no-restore` → `dotnet test BookingSystem.Tests/BookingSystem.Tests.csproj --configuration Release --no-build`. Risultato: 0 errori/warning, **25/25 test superati**.

### Vincoli rispettati
Nessuna modifica al codice applicativo, solo alla definizione della pipeline CI.

## 2026-08-06 — Esternalizzazione configurazione e segreti (IConfiguration / User Secrets / Environment Variables)

Punto di partenza: discussione su come automatizzare build+push dell'immagine Docker in pipeline (serviva scegliere un registry: Azure Container Registry, Docker Hub o GitHub Container Registry). L'utente ha deciso di **lasciare Docker così com'è, solo per uso locale** — nessuna pubblicazione su registry esterni per ora, quindi il relativo punto è stato tolto dai TODO (si può riprendere in futuro se servirà un deploy reale).

Da lì il focus si è spostato su un'analisi completa del progetto per individuare tutti i valori hardcoded che rappresentano configurazione/segreti (da esternalizzare) distinguendoli dalle vere regole di business (da lasciare intatte).

### 1. Analisi (due ricerche in parallelo)
- Segreti/credenziali in chiaro trovati: `Jwt:SecretKey` in `appsettings.json`, `AdminEmail`/`AdminPassword` come `const string` in `IdentitySeeder.cs`, `SA_PASSWORD` (duplicata anche dentro la connection string) in `docker-compose.yml` — tutti già presenti nella cronologia Git.
- Configurazione non sensibile lasciata invariata: `Jwt:Issuer`/`Audience`/`ExpirationMinutes`, connection string di default, porte.
- Regole di business confermate come da **non toccare**: validazione prezzo camera > 0, `CheckIn < CheckOut`, logica di sovrapposizione prenotazioni (`HasOverlapAsync`), enum `RoomDeleteResult`, algoritmo `HmacSha256` del token JWT.
- Trovata anche una duplicazione minore (non un segreto): le stringhe `"Admin"`/`"User"` erano ripetute in `IdentitySeeder.cs`, `AuthService.cs` e negli attributi `[Authorize(Roles = "Admin")]` di `RoomsController.cs`.

### 2. Decisione sulla rotazione dei segreti
L'utente ha scelto esplicitamente di **non rigenerare** JWT SecretKey e password admin (restano gli stessi valori attuali, solo spostati fuori dal codice/file committati) — restano quindi comunque presenti nella cronologia Git pregressa; punto aperto se in futuro si vorrà ruotarli.

### 3. Modifiche applicate
- **User Secrets** (meccanismo di ASP.NET Core per tenere segreti fuori dal repository durante lo sviluppo locale, salvati in un file sulla macchina dell'utente): inizializzati su `BookingSystem.API` (`dotnet user-secrets init`), impostati `Jwt:SecretKey`, `AdminSeed:Email`, `AdminSeed:Password`.
- `appsettings.json`: `Jwt:SecretKey` svuotato, aggiunta sezione `AdminSeed` (Email/Password) vuota.
- Nuova classe `AdminSeedOptions` (stesso pattern già usato da `JwtSettings`) in `BookingSystem.Infrastructure/Identity/`; `IdentitySeeder.SeedAsync` ora riceve queste opzioni invece di leggere `const` interne, e salta la creazione dell'utente admin se email/password non sono configurate (invece di creare un utente con credenziali vuote).
- Nuova classe statica `Roles` (`BookingSystem.Application/Common/Roles.cs`) con le costanti `Admin`/`User`, usata ora in `IdentitySeeder.cs`, `AuthService.cs` e `RoomsController.cs` al posto delle stringhe duplicate — è rimasta hardcoded di proposito (è una vera costante applicativa, richiesta anche a compile-time dagli attributi `[Authorize]`), solo consolidata in un unico punto.
- `Program.cs`: legge la sezione `AdminSeed` da `IConfiguration` e la passa a `IdentitySeeder.SeedAsync`.
- `docker-compose.yml`: `SA_PASSWORD`, `Jwt__SecretKey`, `AdminSeed__Email`, `AdminSeed__Password` ora letti da variabili d'ambiente (convenzione ASP.NET Core: `__` come separatore di sezione) invece di essere scritti in chiaro; eliminata anche la duplicazione della password SA dentro la connection string.
- Creato `.env` (valori reali attuali, **non committato**) e `.env.example` (committato, solo nomi delle variabili senza valori) per documentare cosa serve a chi clona il repo.
- `.gitignore`: aggiunta la riga `.env`.

### 4. Verifica
`dotnet build` sull'intera solution: 0 errori/warning. `dotnet test`: **25/25 test superati** (nessuna modifica ha toccato la logica testata).

### Vincoli rispettati
Nessuna modifica alle regole di business esistenti, nessuna rotazione dei segreti (valori identici a prima, solo spostati), nessun cambiamento di comportamento delle API pubbliche.

## 2026-08-06 — Integrazione Redis come cache (GET /api/rooms)

Punto di partenza: domanda dell'utente se avesse senso aggiungere Redis (un database in-memoria, cioè che tiene i dati in RAM invece che su disco, usato tipicamente per cache) dato che Docker viene usato solo in locale. Chiarito che le due cose sono indipendenti: Redis in Docker Compose in locale è esattamente il modo standard con cui si sviluppa/testa contro Redis anche in aziende reali (in produzione cambia solo la connection string verso un servizio gestito, il codice C# resta identico). L'utente ha poi fornito una specifica dettagliata: cache solo per `GET /api/rooms`, invalidazione su Create/Update/Delete, Redis trattato come dettaglio infrastrutturale (niente dipendenze da Redis nel Domain, niente logica nei Controller), configurazione tramite `IConfiguration`/environment variables.

### Architettura scelta
- **Application** (`BookingSystem.Application/Interfaces/ICacheService.cs`): nuova interfaccia astratta di cache (`GetAsync<T>`/`SetAsync<T>`/`RemoveAsync`), zero riferimenti a Redis o StackExchange.
- **Infrastructure** (`BookingSystem.Infrastructure/Caching/`):
  - `RedisCacheService` — implementa `ICacheService` sopra `IDistributedCache` (l'astrazione standard di ASP.NET Core per cache distribuite), con serializzazione JSON.
  - `CacheSettings` — opzioni configurabili (`RoomsCacheDurationSeconds`, default 60), stesso pattern già usato da `JwtSettings`/`AdminSeedOptions`.
  - `CachedRoomService` — **decorator** che implementa `IRoomService` avvolgendo il vero `RoomService` esistente: intercetta solo `GetAllRoomsAsync` (cache-aside: controlla Redis, se assente legge da SQL Server e popola la cache) e invalida la chiave `rooms:all` quando Create/Update/Delete hanno successo. `RoomService` non è stato toccato — rispetta l'Open/Closed Principle, nessun rischio di rompere la logica di business già testata.
- **API**: `Program.cs` registra `AddStackExchangeRedisCache`, `ICacheService` e sostituisce la registrazione di `IRoomService` con `CachedRoomService` (che avvolge `RoomService` concreto). `RoomsController` non modificato — riceve `IRoomService` come sempre, ignaro della cache.

### Configurazione
`appsettings.json`: aggiunte `ConnectionStrings:Redis` (`localhost:6379`, non è un segreto) e sezione `Caching:RoomsCacheDurationSeconds`. `docker-compose.yml`: nuovo servizio `redis` (immagine `redis:latest`, porta `6379`), variabile `ConnectionStrings__Redis: "redis:6379"` passata al servizio `api`.

### Test
Nuovo `BookingSystem.Tests/Services/CachedRoomServiceTests.cs` (10 test) con mock di `IRoomService` (interno) e `ICacheService`: cache hit non chiama il service interno, cache miss lo chiama e popola la cache, invalidazione su Create/Update/Delete solo se l'operazione ha successo, `GetRoomAsync` passa dritto senza toccare la cache. Aggiunto riferimento (`ProjectReference`) da `BookingSystem.Tests` a `BookingSystem.Infrastructure`, necessario perché `CachedRoomService` vive lì.

### Verifica
`dotnet build`: 0 errori/warning. `dotnet test`: **35/35 test superati** (25 preesistenti + 10 nuovi). Verifica end-to-end con `docker compose up --build`: login admin funzionante, `GET /api/rooms` con cache miss iniziale, ispezione diretta della chiave `rooms:all` su Redis (`redis-cli HGETALL`) che conferma il valore JSON salvato con scadenza, `POST /api/rooms` che invalida correttamente la chiave, richiesta successiva che la ripopola con la nuova stanza, e conferma (via log dell'API) che una richiesta in cache HIT **non** esegue alcuna query SQL sulla tabella `Rooms`.

### Nota aperta
L'immagine Docker usa il tag `redis:latest` (richiesto esplicitamente dall'utente); per riproducibilità in un contesto reale converrebbe fissare una versione esplicita (es. `redis:7-alpine`), da valutare se si vorrà rendere il progetto più vicino a un setup di produzione.

### Vincoli rispettati
Nessuna modifica alla logica di business esistente di `RoomService`, nessuna dipendenza da Redis nel Domain o nei Controller, nessun valore di connessione hardcoded (tutto via `IConfiguration`/environment variables), nessun cambiamento di comportamento delle API pubbliche (stessi endpoint, stesse risposte).

## 2026-08-06 — Analisi estensione caching Redis ad altri endpoint (nessuna modifica al codice)

Punto di partenza: richiesta di valutare se estendere il caching Redis (introdotto nella sessione precedente per `GET /api/rooms`) a `GET /api/rooms/{id}`, `GET /api/bookings` e all'endpoint di verifica disponibilità camere, mantenendo la stessa qualità architetturale e senza overengineering.

### Discrepanze trovate rispetto alla richiesta
- `GET /api/bookings` (lista completa) **non esiste** nel codice: `BookingsController` espone solo `GET /api/bookings/mine` (per l'utente loggato) e `GET /api/bookings/{id}`.
- Non esiste alcuna operazione di "modifica prenotazione" (`IBookingService`/`IBookingRepository` hanno solo Create/Get/GetByUser/Cancel) — la richiesta di gestire l'invalidazione su "modifica di una prenotazione" non ha nulla a cui agganciarsi ad oggi.

### Conclusione: nessuno degli endpoint analizzati giustifica Redis
- **`GET /api/rooms/{id}`** e **`GET /api/bookings/{id}`**: lookup per chiave primaria, già indicizzati, costo trascurabile — cachearli aggiungerebbe una famiglia di chiavi (e relativa invalidazione) senza un collo di bottiglia reale da risolvere.
- **`GET /api/bookings/mine`**: dati per-utente (chiave frammentata per `userId`, beneficio basso), e rischio UX concreto — un utente che cancella una prenotazione e ricarica subito la pagina vedrebbe dati stantii se l'invalidazione fallisse anche una sola volta.
- **Verifica disponibilità (`GET /api/rooms/{roomId}/availability`)**: il caso più netto da escludere — spazio delle chiavi combinatorio (`roomId` × `checkIn` × `checkOut`), invalidazione "esatta" impraticabile senza un indice separato delle chiavi generate per stanza, e rischio di mostrare "disponibile" quando non lo è più (mitigato solo dal fatto che `CreateBookingAsync` rifà comunque il controllo live, quindi nessun rischio di doppia prenotazione — solo un rischio di UX evitabile del tutto non cachando).

L'utente ha confermato che l'analisi è sufficiente così — nessuna riga di codice toccata in questa sessione.

### Nota a margine (non un problema di cache, segnalato ma non corretto)
`GET /api/bookings/{id}` non verifica che la prenotazione richiesta appartenga a chi fa la richiesta — qualunque utente autenticato può leggere il dettaglio di una prenotazione altrui conoscendone l'id. Da valutare in una sessione dedicata all'autorizzazione, se rilevante.

### Vincoli rispettati
Nessuna modifica al codice di produzione, nessun endpoint nuovo creato, nessuna cache aggiunta dove il beneficio non era dimostrabile.

## 2026-08-06 — Rate limiting con Redis su Login, Register e creazione prenotazioni

Punto di partenza: richiesta di proteggere `POST /api/auth/login`, `POST /api/auth/register` e `POST /api/bookings` da abuso (forza bruta sulle credenziali, registrazioni massive automatizzate, spam di richieste di prenotazione), usando Redis (già presente in Infrastructure per il caching) invece di un contatore in-memory, così il limite resterebbe corretto anche con più istanze dell'API in futuro.

### Analisi e soglie scelte
- `POST /api/auth/login` — limite per **IP** (endpoint anonimo): 5 tentativi / 60s.
- `POST /api/auth/register` — limite per **IP**: 5 tentativi / 3600s (finestra più larga perché la registrazione è rara per un utente reale, tollera IP condivisi come uffici/NAT).
- `POST /api/bookings` — limite per **userId** (endpoint autenticato, l'IP non è affidabile qui per via di NAT/proxy condivisi): 10 tentativi / 60s.
- Esclusi `GetBooking`/`GetMyBookings`/`CheckAvailability`: letture già economiche, nessun vettore di abuso paragonabile (coerente con l'analisi caching della sessione precedente).

### Architettura scelta
- **Algoritmo: fixed-window counter** su Redis (`INCR` + `EXPIRE` solo al primo hit della finestra) — `INCR` è atomico lato server, quindi immune a race condition tra richieste concorrenti sulla stessa chiave, a differenza di un get-poi-set fatto a mano. Scartato il sliding-window (richiederebbe Sorted Set + script Lua, sproporzionato qui) e il middleware nativo `Microsoft.AspNetCore.RateLimiting` (i suoi limiter incorporati sono tutti in-memory; un limiter Redis-backed richiederebbe reimplementare a mano la classe astratta `RateLimiter`).
- **Application** (`IRateLimiter.cs`): contratto astratto `IsAllowedAsync(key, limit, window)`, zero riferimenti a Redis.
- **Infrastructure** (`RateLimiting/`): `RedisRateLimiter` implementa il contratto con `IDatabase.StringIncrementAsync`/`KeyExpireAsync` di StackExchange.Redis — non riusa `ICacheService` (quello scritto per il caching) perché espone solo Get/Set/Remove su blob JSON, senza incremento atomico. `RateLimitPolicy` (Limit/WindowSeconds) è il tipo di opzione per policy nominata.
- **API** (`Filters/RateLimitAttribute.cs`): `IAsyncActionFilter` applicato come attributo dichiarativo (`[RateLimit("Login", RateLimitKeyType.IpAddress)]`), stesso principio già seguito per la cache — nessuna logica Redis dentro i Controller. Costruisce la chiave da IP (`HttpContext.Connection.RemoteIpAddress`) o da `userId` (claim `NameIdentifier`), e risponde `429 Too Many Requests` se il limite è superato, senza eseguire l'azione.
- `Program.cs`: registra `IConnectionMultiplexer` come singleton dedicato (separato dalla connessione interna di `AddStackExchangeRedisCache`, che non la espone), `IRateLimiter → RedisRateLimiter`, e la sezione `RateLimiting` di `appsettings.json` come `Dictionary<string, RateLimitPolicy>`.
- `BookingSystem.Infrastructure.csproj`: aggiunto `PackageReference` esplicito a `StackExchange.Redis` (prima solo transitivo).

### Test
Nuovo `BookingSystem.Tests/RateLimiting/RedisRateLimiterTests.cs` (4 test) con mock di `IConnectionMultiplexer`/`IDatabase`: primo hit imposta la scadenza, hit successivi non la resettano, conteggio entro il limite → consentito, oltre il limite → bloccato.

### Verifica
`dotnet build`: 0 errori/warning. `dotnet test`: **39/39** (35 preesistenti + 4 nuovi). End-to-end con `docker compose up --build`: 6 login falliti di fila → i primi 5 rispondono `401` (comportamento business invariato), il 6° `429`; chiave `ratelimit:login:{ip}` su Redis con conteggio e TTL corretti. 11 richieste `POST /api/bookings` con lo stesso utente → la 1ª riesce (`200`), le successive falliscono per sovrapposizione date (`400`, business logic invariata), l'11ª `429`; chiave `ratelimit:createbooking:{userId}` confermata su Redis.

### Vincoli rispettati
Nessuna dipendenza da Redis nel Domain, nessuna logica di rate limiting nei Controller (solo l'attributo dichiarativo), stessa infrastruttura Redis già presente (nessun nuovo servizio in `docker-compose.yml`), nessun cambiamento di comportamento per le richieste entro soglia.

## TODO — prossimi passi

- [ ] **Rotazione dei segreti**: JWT `SecretKey` e password admin di seed restano gli stessi valori già presenti nella cronologia Git da prima della sessione del 2026-08-06 (scelta esplicita dell'utente di non ruotarli). Da rivalutare se il progetto dovesse mai uscire dall'uso puramente locale/didattico.
- [ ] **Ownership su `GET /api/bookings/{id}`**: qualunque utente autenticato può leggere il dettaglio di una prenotazione altrui conoscendone l'id (nessun controllo che `UserId` corrisponda a chi fa la richiesta). Segnalato ma non corretto, da valutare se si vuole affrontare la sicurezza degli endpoint di booking.
- [ ] **`GET /api/bookings` (lista completa, verosimilmente Admin-only)**: non esiste ad oggi. Da creare solo se serve davvero un caso d'uso amministrativo — nessuna decisione presa in merito.

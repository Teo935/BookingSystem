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

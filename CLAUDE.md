# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stile di comunicazione

Sono in fase di apprendimento di .NET/C#. Quando spieghi cosa stai facendo o
perché, evita acronimi e sigle tecniche senza spiegarle (es. non scrivere solo
"DI" ma "Dependency Injection (DI)" almeno la prima volta). Preferisco spiegazioni
dirette e comprensibili anche a chi non ha già tutto il gergo a memoria.

## Cos'è questo progetto

API REST (ASP.NET Core 8) per la gestione di prenotazioni di camere (`Room`) e
prenotazioni (`Booking`), con Entity Framework Core 8 su SQLite. Impedisce
prenotazioni sovrapposte sulla stessa camera e blocca la cancellazione di
camere con prenotazioni attive.

## Comandi comuni

```
dotnet build                                    # compila l'intera solution
dotnet run --project BookingSystem.API          # avvia l'API (Swagger UI in Development su /swagger)
dotnet ef migrations add <Name> --project BookingSystem.Infrastructure --startup-project BookingSystem.API
dotnet ef database update --project BookingSystem.Infrastructure --startup-project BookingSystem.API
```

Non esiste ancora un progetto di test nella solution (`BookingSystem.sln` ha
solo i 4 progetti elencati sotto).

Il database di sviluppo è SQLite (`bookingsystem.db`, definito in
`BookingSystem.API/appsettings.json`); le migration vivono in
`BookingSystem.Infrastructure/Migrations`.

## Architettura

La solution segue la Clean Architecture, divisa in 4 progetti con dipendenze
a senso unico (i livelli interni non conoscono quelli esterni), ed è pensata
come esercizio guidato per applicare i principi SOLID (Single Responsibility,
Open/Closed, Liskov Substitution, Interface Segregation, Dependency
Inversion). Quando proponi modifiche o nuove funzionalità, privilegia
soluzioni che rispettano questi principi (es. nuove interfacce in
Application invece di far dipendere i livelli interni da dettagli
implementativi di Infrastructure) e segnala eventuali violazioni evidenti
che incontri nel codice esistente.

```
BookingSystem.Domain          <- nessuna dipendenza (entità pure: Room, Booking)
BookingSystem.Application     <- dipende da Domain (interfacce, DTO, servizi con la logica di business)
BookingSystem.Infrastructure  <- dipende da Domain + Application (EF Core, repository, migration)
BookingSystem.API             <- dipende da Application + Infrastructure (controller, Program.cs)
```

Il flusso di una richiesta tipica è: `Controller` (BookingSystem.API) →
`IXxxService` (BookingSystem.Application/Interfaces, implementato in
Services) → `IXxxRepository` (interfaccia in Application, implementazione in
Infrastructure/Repositories che usa `AppDbContext`).

Punti da tenere presente quando si modifica la logica:

- I servizi applicativi (`RoomService`, `BookingService`) ritornano tuple
  `(bool Success, string? Error, T? Value)` invece di lanciare eccezioni per
  gli errori di validazione/business (es. camera inesistente, date non
  valide). I controller traducono questi risultati in risposte HTTP
  (`BadRequest`, `NotFound`, ecc.).
- La cancellazione di una camera passa per l'enum `RoomDeleteResult`
  (Success/NotFound/Conflict) in `BookingSystem.Application/Common`, perché
  serve distinguere "non trovata" (404) da "ha prenotazioni attive" (409
  Conflict) — un semplice `bool`/`null` non basterebbe.
- Il controllo di sovrapposizione tra prenotazioni (`HasOverlapAsync` in
  `BookingRepository`) usa la condizione `checkIn < b.CheckOut && checkOut > b.CheckIn`:
  qualunque nuova query sulle date di prenotazione deve rispettare la stessa
  logica di intervallo per restare coerente.
- La registrazione delle dipendenze (Dependency Injection, DI) avviene tutta
  in `BookingSystem.API/Program.cs`: ogni nuovo servizio o repository va
  registrato lì con `AddScoped<Interfaccia, Implementazione>`.

## CI

`azure-pipelines.yml` esegue build/restore/test su Azure Pipelines ad ogni
push su `main` (pipeline ereditata da un template ASP.NET Core generico; al
momento non ci sono test da eseguire).

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:

- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

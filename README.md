# Booking System

Un'API REST per la gestione di prenotazioni di camere, costruita in **ASP.NET Core 8** con **Entity Framework Core** e **SQLite**.

## Cosa fa

- Crea, modifica, elimina camere (`Room`)
- Crea prenotazioni (`Booking`) su una camera per un intervallo di date
- Verifica la disponibilità di una camera in un dato periodo
- Impedisce automaticamente prenotazioni sovrapposte sulla stessa camera
- Blocca la cancellazione di camere con prenotazioni attive

## Stack tecnologico

| Componente | Scelta |
|---|---|
| Framework | ASP.NET Core 8 (Web API, Controllers) |
| Database | SQLite + Entity Framework Core 8 |


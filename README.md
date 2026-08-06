# Booking System

API REST per la gestione di camere e prenotazioni sviluppata con **ASP.NET Core 8**, **Entity Framework Core**, **SQL Server** e progettata seguendo i principi della **Clean Architecture** e **SOLID**.

L'obiettivo del progetto è simulare un'applicazione backend moderna adottando tecnologie comunemente utilizzate in contesti enterprise.

---

# Funzionalità

## Gestione Camere

* Creazione di nuove camere
* Modifica delle camere esistenti
* Eliminazione delle camere
* Visualizzazione dell'elenco camere
* Visualizzazione del dettaglio di una camera
* Blocco dell'eliminazione di camere con prenotazioni associate

## Gestione Prenotazioni

* Creazione di prenotazioni
* Verifica della disponibilità delle camere
* Prevenzione automatica delle prenotazioni sovrapposte
* Visualizzazione delle prenotazioni dell'utente autenticato
* Cancellazione delle prenotazioni

## Autenticazione e Autorizzazione

* Registrazione utenti
* Login tramite JWT
* ASP.NET Core Identity
* Role-Based Authorization
* Gestione dei ruoli:

  * Admin
  * User

## Sicurezza

* Password gestite da ASP.NET Core Identity
* JWT Bearer Authentication
* Endpoint protetti tramite Authorization
* Gestione dei segreti tramite variabili d'ambiente
* Configurazione esterna tramite IConfiguration
* Rate Limiting sugli endpoint sensibili

## Performance

* Distributed Cache tramite Redis
* Cache automatica dell'elenco delle camere
* Cache Hit / Cache Miss
* Cache Invalidation automatica quando vengono create, aggiornate o eliminate camere

---

# Architettura

Il progetto segue la **Clean Architecture**.

```text
BookingSystem.API
│
├── BookingSystem.Application
├── BookingSystem.Domain
└── BookingSystem.Infrastructure
```

## API Layer

Responsabile di:

* Controller
* Endpoint HTTP
* Configurazione Authentication e Authorization
* Rate Limiting
* Dependency Injection

## Application Layer

Responsabile di:

* Services
* Business Workflow
* DTO
* Interfaces
* Validazioni
* Gestione della cache tramite servizi applicativi

## Domain Layer

Responsabile di:

* Entità
* Enum
* Regole di business
* Modello di dominio

## Infrastructure Layer

Responsabile di:

* Entity Framework Core
* SQL Server
* Redis
* ASP.NET Core Identity
* Repository
* Persistenza dati
* Configurazioni esterne

---

# Stack Tecnologico

| Componente             | Tecnologia                 |
| ---------------------- | -------------------------- |
| Framework              | ASP.NET Core 8 Web API     |
| Linguaggio             | C#                         |
| Architettura           | Clean Architecture         |
| Principi               | SOLID                      |
| Database               | SQL Server                 |
| ORM                    | Entity Framework Core 8    |
| Cache                  | Redis                      |
| Autenticazione         | ASP.NET Core Identity      |
| Authorization          | JWT + Roles                |
| Rate Limiting          | ASP.NET Core Rate Limiting |
| Dependency Injection   | Built-in ASP.NET Core DI   |
| Containerizzazione     | Docker + Docker Compose    |
| Testing                | xUnit + Moq                |
| Continuous Integration | Azure DevOps Pipelines     |
| Versionamento          | Git + GitHub               |

---

# Autenticazione

## Registrazione

```http
POST /api/auth/register
```

## Login

```http
POST /api/auth/login
```

Il login restituisce un JWT utilizzabile per accedere agli endpoint protetti.

---

# Ruoli

## User

Può:

* Visualizzare le camere
* Creare prenotazioni
* Visualizzare le proprie prenotazioni

## Admin

Può:

* Creare camere
* Modificare camere
* Eliminare camere
* Gestire le risorse dell'applicazione

---

# Sicurezza

Gli endpoint sono protetti tramite:

```csharp
[Authorize]
```

oppure:

```csharp
[Authorize(Roles = "Admin")]
```

Gli endpoint più sensibili sono inoltre protetti tramite **Rate Limiting**, limitando il numero di richieste consentite in un intervallo di tempo per prevenire abusi e attacchi di tipo brute-force.

---

# Cache con Redis

Il progetto utilizza **Redis** come sistema di caching distribuito per migliorare le performance delle operazioni di lettura.

Attualmente la cache è applicata a:

* `GET /api/rooms`

La strategia implementata prevede:

* Cache Hit
* Cache Miss
* Cache Invalidation automatica in caso di:

  * creazione di una camera;
  * modifica di una camera;
  * eliminazione di una camera.

SQL Server rimane l'unica fonte di verità dei dati.

---

# Database

Il progetto utilizza:

* SQL Server
* Entity Framework Core Migrations
* ASP.NET Core Identity

Le migrazioni vengono utilizzate per la gestione dello schema del database.

---

# Docker

L'applicazione può essere eseguita tramite Docker e Docker Compose.

```bash
docker compose up --build
```

Container utilizzati:

* BookingSystem API
* SQL Server
* Redis

---

# Continuous Integration

Il progetto include una pipeline di **Azure DevOps** che esegue automaticamente:

* Restore delle dipendenze
* Build dell'applicazione
* Esecuzione degli Unit Test
* Verifica della corretta compilazione ad ogni push sul repository

---

# Testing

Il progetto include Unit Test sviluppati con:

* xUnit
* Moq

I test verificano il comportamento della business logic simulando le dipendenze tramite mock.

---

# Obiettivi del Progetto

Questo progetto è stato realizzato per approfondire tecnologie e pattern utilizzati nello sviluppo backend moderno, tra cui:

* ASP.NET Core Web API
* Clean Architecture
* SOLID Principles
* Entity Framework Core
* SQL Server
* ASP.NET Core Identity
* JWT Authentication
* Role-Based Authorization
* Redis Distributed Cache
* ASP.NET Core Rate Limiting
* Docker
* Docker Compose
* Azure DevOps Pipelines
* Unit Testing
* Dependency Injection
* Architetture backend moderne

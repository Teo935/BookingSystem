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

## Messaggistica Asincrona (Event-Driven)

* Pubblicazione di eventi di dominio su **RabbitMQ** alla creazione e alla cancellazione di una prenotazione
* Consumer disaccoppiato che elabora gli eventi in modo asincrono, simulando l'invio di notifiche via email
* La disponibilità del broker non influisce mai sull'esito della richiesta HTTP (eventuali errori di pubblicazione vengono solo loggati)

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
* RabbitMQ
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
| Messaggistica          | RabbitMQ (Event-Driven)    |
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

```json
{
  "email": "utente@example.com",
  "password": "Password123!"
}
```

Il nuovo utente viene creato con il ruolo `User`.

## Login

```http
POST /api/auth/login
```

```json
{
  "email": "utente@example.com",
  "password": "Password123!"
}
```

Il login restituisce un JWT (`token`) e un refresh token (`refreshToken`) utilizzabili per accedere agli endpoint protetti.

Per provare gli endpoint riservati all'Admin (creazione/modifica/eliminazione camere), effettuare il login con le credenziali configurate in `.env` (`ADMIN_SEED_EMAIL` / `ADMIN_SEED_PASSWORD`): l'utente Admin viene creato automaticamente all'avvio dell'applicazione.

Per usare il token in Swagger: copiare il valore di `token` dalla risposta di login, cliccare sul pulsante **Authorize** in alto e inserirlo nel formato `Bearer <token>`.

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

# Eventi con RabbitMQ

Il progetto utilizza **RabbitMQ** per disaccoppiare, tramite un pattern **Event-Driven**, la creazione e la cancellazione di una prenotazione dall'invio delle relative notifiche.

Topologia:

* Un exchange di tipo `topic` (`booking.events`)
* Due routing key (`booking.created`, `booking.cancelled`)
* Una coda (`booking.notifications`) con un consumer dedicato

Flusso:

* `POST /api/bookings` e `DELETE /api/bookings/{id}` pubblicano un evento su RabbitMQ dopo il successo dell'operazione
* Un consumer, eseguito in background nello stesso processo dell'API, elabora l'evento in modo asincrono simulando l'invio di un'email di conferma/cancellazione
* Se il broker non è raggiungibile, la pubblicazione fallisce in modo silenzioso (solo un log di warning): la richiesta HTTP e la business logic non ne risentono mai

La Management UI di RabbitMQ è disponibile su `http://localhost:15672` quando l'applicazione è eseguita tramite Docker Compose.

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

Prerequisito: creare un file `.env` nella root del repository (partendo da `.env.example`) con valori per `SA_PASSWORD`, `JWT_SECRET_KEY`, `ADMIN_SEED_EMAIL`, `ADMIN_SEED_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`.

```bash
docker compose up --build
```

Ad avvio completato (i log mostrano `Now listening on: http://[::]:8080` senza errori):

* **Swagger UI**: [http://localhost:5068/swagger](http://localhost:5068/swagger)
* **RabbitMQ Management UI**: [http://localhost:15672](http://localhost:15672) (credenziali da `RABBITMQ_USER`/`RABBITMQ_PASSWORD` in `.env`)

Container utilizzati:

* BookingSystem API — porta host `5068` (mappata sulla `8080` interna al container)
* SQL Server — porta `1433`
* Redis — porta `6379`
* RabbitMQ — porta `5672` (AMQP) e `15672` (Management UI)

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
* RabbitMQ ed Event-Driven Architecture
* ASP.NET Core Rate Limiting
* Docker
* Docker Compose
* Azure DevOps Pipelines
* Unit Testing
* Dependency Injection
* Architetture backend moderne

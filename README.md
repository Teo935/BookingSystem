# Booking System

API REST per la gestione di camere e prenotazioni sviluppata con **ASP.NET Core 8**, **Entity Framework Core**, **SQL Server** e **Clean Architecture**.

## Funzionalità

### Gestione Camere

* Creazione di nuove camere
* Modifica delle camere esistenti
* Eliminazione delle camere
* Visualizzazione dell'elenco camere
* Visualizzazione dettagli di una camera

### Gestione Prenotazioni

* Creazione prenotazioni
* Verifica disponibilità camere per intervalli di date
* Prevenzione delle prenotazioni sovrapposte
* Visualizzazione delle prenotazioni dell'utente autenticato
* Blocco dell'eliminazione di camere con prenotazioni associate

### Autenticazione e Autorizzazione

* Registrazione utenti
* Login tramite JWT
* ASP.NET Core Identity
* Role-Based Authorization
* Ruoli predefiniti:

  * Admin
  * User

### Sicurezza

* Password memorizzate tramite ASP.NET Core Identity
* JWT Bearer Authentication
* Protezione degli endpoint tramite autorizzazione basata sui ruoli
* Gestione dei segreti tramite configurazione esterna e variabili d'ambiente

---

## Architettura

Il progetto segue i principi della **Clean Architecture** e **SOLID**.

```text
BookingSystem.API
│
├── BookingSystem.Application
├── BookingSystem.Domain
├── BookingSystem.Infrastructure
```

### API Layer

Responsabile di:

* Controller
* Endpoint HTTP
* Configurazione Authentication e Authorization
* Dependency Injection

### Application Layer

Responsabile di:

* Use Cases
* Services
* DTO
* Interfacce
* Business Workflow

### Domain Layer

Responsabile di:

* Entità
* Enum
* Regole di business
* Modello di dominio

### Infrastructure Layer

Responsabile di:

* Entity Framework Core
* SQL Server
* ASP.NET Core Identity
* Repository
* Persistenza dati
* Configurazioni esterne

---

## Stack Tecnologico

| Componente           | Tecnologia               |
| -------------------- | ------------------------ |
| Framework            | ASP.NET Core 8 Web API   |
| Linguaggio           | C#                       |
| Architettura         | Clean Architecture       |
| Principi             | SOLID                    |
| Database             | SQL Server               |
| ORM                  | Entity Framework Core 8  |
| Autenticazione       | ASP.NET Core Identity    |
| Authorization        | JWT + Roles              |
| Dependency Injection | Built-in ASP.NET Core DI |
| Containerizzazione   | Docker                   |
| Testing              | xUnit + Moq              |
| Versionamento        | Git + GitHub             |

---

## Autenticazione

### Registrazione

```http
POST /api/auth/register
```

### Login

```http
POST /api/auth/login
```

Il login restituisce un JWT utilizzabile per accedere agli endpoint protetti.

---

## Ruoli

### User

Può:

* Visualizzare camere
* Creare prenotazioni
* Visualizzare le proprie prenotazioni

### Admin

Può:

* Creare camere
* Modificare camere
* Eliminare camere
* Gestire le risorse dell'applicazione

---

## Sicurezza

Gli endpoint sono protetti tramite:

```csharp
[Authorize]
```

oppure:

```csharp
[Authorize(Roles = "Admin")]
```

---

## Database

Il progetto utilizza:

* SQL Server
* Entity Framework Core Migrations
* ASP.NET Core Identity

Le migrazioni vengono utilizzate per la gestione dello schema del database.

---

## Docker

L'applicazione può essere eseguita tramite Docker e Docker Compose.

```bash
docker compose up --build
```

Container previsti:

* BookingSystem API
* SQL Server

---

## Obiettivi del Progetto

Questo progetto è stato realizzato per approfondire:

* Clean Architecture
* SOLID Principles
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* ASP.NET Core Identity
* JWT Authentication
* Role-Based Authorization
* Docker
* Unit Testing
* Dependency Injection
* Architetture backend moderne

```
```

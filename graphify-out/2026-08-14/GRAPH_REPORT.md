# Graph Report - BookingSystem  (2026-08-06)

## Corpus Check
- 56 files · ~12,299 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 431 nodes · 824 edges · 13 communities
- Extraction: 89% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 86 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a1443197`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Booking Domain & Service
- Room Domain & Service
- Architecture Docs & Session Log
- Auth Login/Register Flow
- Room API Endpoints
- Project Files & NuGet Deps
- Launch Settings Config
- Booking API Endpoints
- DI Wiring & Namespaces
- Identity & JWT Infrastructure
- EF Core Migrations
- Project Overview & DB Migration Rationale
- JWT e ASP.NET Core Identity nel progetto — spiegazione

## God Nodes (most connected - your core abstractions)
1. `Room` - 25 edges
2. `BookingSystem.Application.Interfaces` - 20 edges
3. `Booking` - 20 edges
4. `BookingSystem.Application.DTOs` - 19 edges
5. `RoomServiceTests` - 16 edges
6. `BookingSystem.Domain.Entities` - 15 edges
7. `BookingServiceTests` - 15 edges
8. `BookingSystem.Infrastructure` - 13 edges
9. `CachedRoomServiceTests` - 13 edges
10. `CachedRoomService` - 11 edges

## Surprising Connections (you probably didn't know these)
- `Booking System (README project description)` --semantically_similar_to--> `BookingSystem — ASP.NET Core 8 REST API for Room/Booking management`  [INFERRED] [semantically similar]
  README.md → CLAUDE.md
- `Booking System (README project description)` --semantically_similar_to--> `Room (domain entity)`  [INFERRED] [semantically similar]
  README.md → CLAUDE.md
- `Booking System (README project description)` --semantically_similar_to--> `Booking (domain entity)`  [INFERRED] [semantically similar]
  README.md → CLAUDE.md
- `Tech stack per README: ASP.NET Core 8 (Web API, Controllers) + SQLite + EF Core 8` --semantically_similar_to--> `Entity Framework Core 8 (ORM)`  [INFERRED] [semantically similar]
  README.md → CLAUDE.md
- `Tech stack per README: ASP.NET Core 8 (Web API, Controllers) + SQLite + EF Core 8` --semantically_similar_to--> `SQLite (dev database, per CLAUDE.md)`  [INFERRED] [semantically similar]
  README.md → CLAUDE.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Layered Request Flow (Controller → Service → Repository → AppDbContext)** — claude_controller_generic, claude_ixxxservice_generic, claude_ixxxrepository_generic, claude_appdbcontext_generic [EXTRACTED 1.00]
- **4-Project Clean Architecture Split** — claude_domain_project, claude_application_project, claude_infrastructure_project, claude_api_project [EXTRACTED 1.00]
- **ASP.NET Core Identity + JWT Authentication Flow** — context_sessions_authcontroller, context_sessions_authservice, context_sessions_jwttokengenerator, context_sessions_applicationuser, context_sessions_identityseeder [EXTRACTED 0.95]

## Communities (13 total, 0 thin omitted)

### Community 0 - "Booking Domain & Service"
Cohesion: 0.10
Nodes (20): IBookingRepository, DateTime, IEnumerable, Task, BookingService, DateTime, Error, IEnumerable (+12 more)

### Community 1 - "Room Domain & Service"
Cohesion: 0.11
Nodes (16): IRoomRepository, List, Task, RoomService, Error, List, Success, Task (+8 more)

### Community 2 - "Architecture Docs & Session Log"
Cohesion: 0.06
Nodes (51): ASP.NET Core CI pipeline (Azure Pipelines, trigger: main, windows-latest), NuGetCommand@2 — restore solution packages, VSBuild@1 — build/publish solution, VSTest@2 — run tests, BookingSystem.API (depends on Application+Infrastructure; controllers, Program.cs), AppDbContext (EF Core) — generic request-flow role, BookingSystem.Application (depends on Domain; interfaces, DTOs, services), Booking (domain entity) (+43 more)

### Community 3 - "Auth Login/Register Flow"
Cohesion: 0.10
Nodes (22): AuthController, HttpPost, IActionResult, Task, AuthResponse, DateTime, IEnumerable, LoginRequest (+14 more)

### Community 4 - "Room API Endpoints"
Cohesion: 0.06
Nodes (33): Authorize, RoomsController, HttpDelete, HttpGet, HttpPost, IActionResult, Task, RoomDeleteResult (+25 more)

### Community 5 - "Project Files & NuGet Deps"
Cohesion: 0.08
Nodes (29): BookingSystem.API, net8.0, Microsoft.EntityFrameworkCore.Design (8.0.11), BookingSystem.Application, net8.0, Microsoft.NET.Sdk, BookingSystem.Domain, net8.0 (+21 more)

### Community 6 - "Launch Settings Config"
Cohesion: 0.07
Nodes (28): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+20 more)

### Community 7 - "Booking API Endpoints"
Cohesion: 0.12
Nodes (17): AllowAnonymous, BookingsController, DateTime, HttpDelete, HttpGet, HttpPost, IActionResult, Task (+9 more)

### Community 8 - "DI Wiring & Namespaces"
Cohesion: 0.14
Nodes (13): Roles, string, CacheSettings, BookingSystem.Infrastructure.Caching, BookingSystem.Domain.Entities, BookingSystem.Infrastructure.Data, BookingSystem.Infrastructure.Repositories, BookingSystem.Application.Common (+5 more)

### Community 9 - "Identity & JWT Infrastructure"
Cohesion: 0.08
Nodes (19): AppDbContext, ModelBuilder, AdminSeedOptions, ApplicationUser, IdentitySeeder, Task, UserManager, JwtSettings (+11 more)

### Community 10 - "EF Core Migrations"
Cohesion: 0.09
Nodes (13): InitialCreate, MigrationBuilder, InitialCreate, ModelBuilder, AddIdentityAndBookingUserId, MigrationBuilder, AddIdentityAndBookingUserId, ModelBuilder (+5 more)

### Community 11 - "Project Overview & DB Migration Rationale"
Cohesion: 0.32
Nodes (4): RedisCacheService, Task, TimeSpan, IDistributedCache

### Community 12 - "JWT e ASP.NET Core Identity nel progetto — spiegazione"
Cohesion: 0.33
Nodes (5): 1. ASP.NET Core Identity — cos'è e come lo abbiamo usato, 2. I Ruoli (Admin e User), 3. Il Token JWT — come viene generato e validato, 4. Perché questi file sono in `Infrastructure/Identity` e non in `Application`, JWT e ASP.NET Core Identity nel progetto — spiegazione

## Ambiguous Edges - Review These
- `CI note in CLAUDE.md: azure-pipelines.yml runs build/restore/test on push to main; doc states 'no tests to run yet' (possibly stale, see BookingSystem.Tests)` → `BookingSystem.Tests (xUnit, net8.0, ProjectReference only to Application, created 2026-07-21)`  [AMBIGUOUS]
  CLAUDE.md · relation: conceptually_related_to

## Knowledge Gaps
- **53 isolated node(s):** `net8.0`, `Swashbuckle.AspNetCore (6.6.2)`, `Microsoft.AspNetCore.Authentication.JwtBearer (8.0.11)`, `Microsoft.EntityFrameworkCore.Design (8.0.11)`, `Microsoft.NET.Sdk.Web` (+48 more)
  These have ≤1 connection - possible missing edges or undocumented components.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `CI note in CLAUDE.md: azure-pipelines.yml runs build/restore/test on push to main; doc states 'no tests to run yet' (possibly stale, see BookingSystem.Tests)` and `BookingSystem.Tests (xUnit, net8.0, ProjectReference only to Application, created 2026-07-21)`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `Room` connect `Room Domain & Service` to `Booking Domain & Service`, `DI Wiring & Namespaces`, `Room API Endpoints`, `Identity & JWT Infrastructure`?**
  _High betweenness centrality (0.125) - this node is a cross-community bridge._
- **Why does `BookingSystem.Application.Interfaces` connect `DI Wiring & Namespaces` to `Project Overview & DB Migration Rationale`, `Auth Login/Register Flow`, `Room API Endpoints`, `Booking API Endpoints`?**
  _High betweenness centrality (0.094) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `Identity & JWT Infrastructure` to `Booking Domain & Service`, `Room Domain & Service`?**
  _High betweenness centrality (0.090) - this node is a cross-community bridge._
- **What connects `net8.0`, `Swashbuckle.AspNetCore (6.6.2)`, `Microsoft.AspNetCore.Authentication.JwtBearer (8.0.11)` to the rest of the system?**
  _53 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Booking Domain & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.0977891156462585 - nodes in this community are weakly interconnected._
- **Should `Room Domain & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.11207729468599034 - nodes in this community are weakly interconnected._
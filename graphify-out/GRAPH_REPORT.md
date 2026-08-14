# Graph Report - BookingSystem  (2026-08-14)

## Corpus Check
- 71 files · ~17,683 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 545 nodes · 1053 edges · 17 communities
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 113 edges (avg confidence: 0.81)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `6cbab069`
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
- .IsAllowedAsync
- Redis come cache per le stanze — spiegazione
- RateLimitAttribute

## God Nodes (most connected - your core abstractions)
1. `BookingSystem.Application.Interfaces` - 26 edges
2. `Room` - 25 edges
3. `BookingSystem.Application.DTOs` - 21 edges
4. `Booking` - 20 edges
5. `BookingServiceTests` - 19 edges
6. `RoomServiceTests` - 16 edges
7. `BookingSystem.Application.Common` - 15 edges
8. `BookingSystem.Domain.Entities` - 15 edges
9. `BookingSystem.Infrastructure` - 14 edges
10. `AuthService` - 13 edges

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

## Communities (17 total, 0 thin omitted)

### Community 0 - "Booking Domain & Service"
Cohesion: 0.09
Nodes (21): IBookingRepository, DateTime, IEnumerable, Task, BookingService, DateTime, Error, IEnumerable (+13 more)

### Community 1 - "Room Domain & Service"
Cohesion: 0.11
Nodes (16): IRoomRepository, List, Task, RoomService, Error, List, Success, Task (+8 more)

### Community 2 - "Architecture Docs & Session Log"
Cohesion: 0.06
Nodes (51): ASP.NET Core CI pipeline (Azure Pipelines, trigger: main, windows-latest), NuGetCommand@2 — restore solution packages, VSBuild@1 — build/publish solution, VSTest@2 — run tests, BookingSystem.API (depends on Application+Infrastructure; controllers, Program.cs), AppDbContext (EF Core) — generic request-flow role, BookingSystem.Application (depends on Domain; interfaces, DTOs, services), Booking (domain entity) (+43 more)

### Community 3 - "Auth Login/Register Flow"
Cohesion: 0.08
Nodes (28): AuthResponse, DateTime, IEnumerable, IRefreshTokenStore, Task, TimeSpan, ApplicationUser, AuthService (+20 more)

### Community 4 - "Room API Endpoints"
Cohesion: 0.06
Nodes (37): Authorize, RoomsController, HttpDelete, HttpGet, HttpPost, IActionResult, Task, RoomDeleteResult (+29 more)

### Community 5 - "Project Files & NuGet Deps"
Cohesion: 0.08
Nodes (30): BookingSystem.API, net8.0, Microsoft.EntityFrameworkCore.Design (8.0.11), BookingSystem.Application, net8.0, Microsoft.NET.Sdk, BookingSystem.Domain, net8.0 (+22 more)

### Community 6 - "Launch Settings Config"
Cohesion: 0.07
Nodes (28): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+20 more)

### Community 7 - "Booking API Endpoints"
Cohesion: 0.11
Nodes (19): AllowAnonymous, BookingsController, DateTime, HttpDelete, HttpGet, HttpPost, IActionResult, RateLimit (+11 more)

### Community 8 - "DI Wiring & Namespaces"
Cohesion: 0.07
Nodes (27): Roles, string, CacheSettings, AppDbContext, ModelBuilder, AdminSeedOptions, IdentitySeeder, Task (+19 more)

### Community 9 - "Identity & JWT Infrastructure"
Cohesion: 0.17
Nodes (14): AuthController, HttpPost, IActionResult, RateLimit, Task, LoginRequest, RefreshTokenRequest, RegisterRequest (+6 more)

### Community 10 - "EF Core Migrations"
Cohesion: 0.09
Nodes (13): InitialCreate, MigrationBuilder, InitialCreate, ModelBuilder, AddIdentityAndBookingUserId, MigrationBuilder, AddIdentityAndBookingUserId, ModelBuilder (+5 more)

### Community 11 - "Project Overview & DB Migration Rationale"
Cohesion: 0.21
Nodes (9): RedisRefreshTokenStore, IConnectionMultiplexer, string, Task, TimeSpan, RedisRefreshTokenStoreTests, Fact, Mock (+1 more)

### Community 12 - "JWT e ASP.NET Core Identity nel progetto — spiegazione"
Cohesion: 0.33
Nodes (5): 1. ASP.NET Core Identity — cos'è e come lo abbiamo usato, 2. I Ruoli (Admin e User), 3. Il Token JWT — come viene generato e validato, 4. Perché questi file sono in `Infrastructure/Identity` e non in `Application`, JWT e ASP.NET Core Identity nel progetto — spiegazione

### Community 13 - ".IsAllowedAsync"
Cohesion: 0.12
Nodes (14): IRateLimiter, Task, TimeSpan, RateLimitPolicy, RedisRateLimiter, IConnectionMultiplexer, Task, TimeSpan (+6 more)

### Community 14 - "Redis come cache per le stanze — spiegazione"
Cohesion: 0.15
Nodes (12): 1. Dove vive Redis — `docker-compose.yml`, 2. Come l'API sa dove si trova Redis — configurazione, 3. Il "contratto" — `ICacheService` (Application), 4. L'implementazione reale — `RedisCacheService` (Infrastructure), 5. La configurazione della durata — `CacheSettings` (Infrastructure), 6. Il cuore della logica — `CachedRoomService` (Infrastructure), 7. Il collante — `Program.cs`, 8. `RoomsController` — invariato (+4 more)

### Community 15 - "RateLimitAttribute"
Cohesion: 0.20
Nodes (8): ActionExecutingContext, ActionExecutionDelegate, Attribute, RateLimitAttribute, string, Task, RateLimitKeyType, IAsyncActionFilter

## Ambiguous Edges - Review These
- `CI note in CLAUDE.md: azure-pipelines.yml runs build/restore/test on push to main; doc states 'no tests to run yet' (possibly stale, see BookingSystem.Tests)` → `BookingSystem.Tests (xUnit, net8.0, ProjectReference only to Application, created 2026-07-21)`  [AMBIGUOUS]
  CLAUDE.md · relation: conceptually_related_to

## Knowledge Gaps
- **67 isolated node(s):** `net8.0`, `Swashbuckle.AspNetCore (6.6.2)`, `Microsoft.AspNetCore.Authentication.JwtBearer (8.0.11)`, `Microsoft.EntityFrameworkCore.Design (8.0.11)`, `Microsoft.NET.Sdk.Web` (+62 more)
  These have ≤1 connection - possible missing edges or undocumented components.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `CI note in CLAUDE.md: azure-pipelines.yml runs build/restore/test on push to main; doc states 'no tests to run yet' (possibly stale, see BookingSystem.Tests)` and `BookingSystem.Tests (xUnit, net8.0, ProjectReference only to Application, created 2026-07-21)`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `BookingSystem.Application.Interfaces` connect `DI Wiring & Namespaces` to `Project Overview & DB Migration Rationale`, `Auth Login/Register Flow`, `Room API Endpoints`, `.IsAllowedAsync`?**
  _High betweenness centrality (0.192) - this node is a cross-community bridge._
- **Why does `Room` connect `Room Domain & Service` to `Booking Domain & Service`, `DI Wiring & Namespaces`, `Room API Endpoints`?**
  _High betweenness centrality (0.089) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `DI Wiring & Namespaces` to `Booking Domain & Service`, `Room Domain & Service`, `Auth Login/Register Flow`?**
  _High betweenness centrality (0.077) - this node is a cross-community bridge._
- **What connects `net8.0`, `Swashbuckle.AspNetCore (6.6.2)`, `Microsoft.AspNetCore.Authentication.JwtBearer (8.0.11)` to the rest of the system?**
  _67 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Booking Domain & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.08956228956228957 - nodes in this community are weakly interconnected._
- **Should `Room Domain & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.10823311748381129 - nodes in this community are weakly interconnected._
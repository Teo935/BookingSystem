# Graph Report - .  (2026-07-25)

## Corpus Check
- Corpus is ~8,116 words - fits in a single context window. You may not need a graph.

## Summary
- 372 nodes · 699 edges · 12 communities
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 68 edges (avg confidence: 0.82)
- Token cost: 85,537 input · 0 output

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

## God Nodes (most connected - your core abstractions)
1. `Room` - 21 edges
2. `Booking` - 20 edges
3. `BookingSystem.Application.DTOs` - 17 edges
4. `BookingSystem.Application.Interfaces` - 16 edges
5. `RoomServiceTests` - 16 edges
6. `BookingServiceTests` - 15 edges
7. `BookingSystem.Domain.Entities` - 13 edges
8. `BookingSystem.Infrastructure` - 11 edges
9. `ASP.NET Core Identity + JWT Authentication + Role-Based Authorization integration (2026-07-21)` - 11 edges
10. `IRoomRepository` - 10 edges

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

## Communities (12 total, 0 thin omitted)

### Community 0 - "Booking Domain & Service"
Cohesion: 0.09
Nodes (20): IBookingRepository, DateTime, IEnumerable, Task, BookingService, DateTime, Error, IEnumerable (+12 more)

### Community 1 - "Room Domain & Service"
Cohesion: 0.11
Nodes (16): IRoomRepository, List, Task, RoomService, Error, List, Success, Task (+8 more)

### Community 2 - "Architecture Docs & Session Log"
Cohesion: 0.09
Nodes (37): ASP.NET Core CI pipeline (Azure Pipelines, trigger: main, windows-latest), NuGetCommand@2 — restore solution packages, VSBuild@1 — build/publish solution, VSTest@2 — run tests, BookingSystem.API (depends on Application+Infrastructure; controllers, Program.cs), AppDbContext (EF Core) — generic request-flow role, BookingSystem.Application (depends on Domain; interfaces, DTOs, services), CI note in CLAUDE.md: azure-pipelines.yml runs build/restore/test on push to main; doc states 'no tests to run yet' (possibly stale, see BookingSystem.Tests) (+29 more)

### Community 3 - "Auth Login/Register Flow"
Cohesion: 0.11
Nodes (22): AuthController, HttpPost, IActionResult, Task, AuthResponse, DateTime, IEnumerable, LoginRequest (+14 more)

### Community 4 - "Room API Endpoints"
Cohesion: 0.12
Nodes (16): Authorize, RoomsController, HttpDelete, HttpGet, HttpPost, IActionResult, Task, RoomDeleteResult (+8 more)

### Community 5 - "Project Files & NuGet Deps"
Cohesion: 0.08
Nodes (28): BookingSystem.API, net8.0, Microsoft.EntityFrameworkCore.Design (8.0.11), BookingSystem.Application, net8.0, Microsoft.NET.Sdk, BookingSystem.Domain, net8.0 (+20 more)

### Community 6 - "Launch Settings Config"
Cohesion: 0.07
Nodes (28): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+20 more)

### Community 7 - "Booking API Endpoints"
Cohesion: 0.12
Nodes (17): AllowAnonymous, BookingsController, DateTime, HttpDelete, HttpGet, HttpPost, IActionResult, Task (+9 more)

### Community 8 - "DI Wiring & Namespaces"
Cohesion: 0.18
Nodes (10): BookingSystem.Domain.Entities, BookingSystem.Infrastructure.Data, BookingSystem.Infrastructure.Repositories, BookingSystem.Application.Common, BookingSystem.API.Controllers, BookingSystem.Tests.Services, BookingSystem.Application.DTOs, BookingSystem.Application.Services (+2 more)

### Community 9 - "Identity & JWT Infrastructure"
Cohesion: 0.08
Nodes (18): AppDbContext, ModelBuilder, ApplicationUser, IdentitySeeder, string, Task, UserManager, JwtSettings (+10 more)

### Community 10 - "EF Core Migrations"
Cohesion: 0.09
Nodes (13): InitialCreate, MigrationBuilder, InitialCreate, ModelBuilder, AddIdentityAndBookingUserId, MigrationBuilder, AddIdentityAndBookingUserId, ModelBuilder (+5 more)

### Community 11 - "Project Overview & DB Migration Rationale"
Cohesion: 0.19
Nodes (14): Booking (domain entity), BookingSystem — ASP.NET Core 8 REST API for Room/Booking management, Clean Architecture (layered, one-way inner-to-outer dependencies), Entity Framework Core 8 (ORM), Room (domain entity), SOLID Principles (SRP, OCP, LSP, ISP, DIP), SQLite (dev database, per CLAUDE.md), Rationale: Booking.UserId added as a nullable string with no real foreign key/navigation property to ApplicationUser, so BookingSystem.Domain doesn't depend on Identity types (+6 more)

## Ambiguous Edges - Review These
- `CI note in CLAUDE.md: azure-pipelines.yml runs build/restore/test on push to main; doc states 'no tests to run yet' (possibly stale, see BookingSystem.Tests)` → `BookingSystem.Tests (xUnit, net8.0, ProjectReference only to Application, created 2026-07-21)`  [AMBIGUOUS]
  CLAUDE.md · relation: conceptually_related_to

## Knowledge Gaps
- **47 isolated node(s):** `net8.0`, `Swashbuckle.AspNetCore (6.6.2)`, `Microsoft.AspNetCore.Authentication.JwtBearer (8.0.11)`, `Microsoft.EntityFrameworkCore.Design (8.0.11)`, `Microsoft.NET.Sdk.Web` (+42 more)
  These have ≤1 connection - possible missing edges or undocumented components.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `CI note in CLAUDE.md: azure-pipelines.yml runs build/restore/test on push to main; doc states 'no tests to run yet' (possibly stale, see BookingSystem.Tests)` and `BookingSystem.Tests (xUnit, net8.0, ProjectReference only to Application, created 2026-07-21)`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `Room` connect `Room Domain & Service` to `Booking Domain & Service`, `Identity & JWT Infrastructure`, `Room API Endpoints`?**
  _High betweenness centrality (0.105) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `Identity & JWT Infrastructure` to `DI Wiring & Namespaces`, `Booking Domain & Service`, `Room Domain & Service`?**
  _High betweenness centrality (0.105) - this node is a cross-community bridge._
- **Why does `Booking` connect `Booking Domain & Service` to `Room Domain & Service`, `Identity & JWT Infrastructure`, `Booking API Endpoints`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **What connects `net8.0`, `Swashbuckle.AspNetCore (6.6.2)`, `Microsoft.AspNetCore.Authentication.JwtBearer (8.0.11)` to the rest of the system?**
  _47 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Booking Domain & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.0946938775510204 - nodes in this community are weakly interconnected._
- **Should `Room Domain & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.10823311748381129 - nodes in this community are weakly interconnected._
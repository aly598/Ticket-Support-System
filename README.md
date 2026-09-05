# Ticket Support System

A secure support ticket workflow API built with ASP.NET Core 8, following Clean Architecture principles.

## Overview

Customers create support tickets through a REST API, support agents claim and resolve them, and administrators oversee the process. The system enforces JWT authentication, role-based authorization, ownership privacy, workflow state machine rules, optimistic concurrency control, and full audit history.

## Technology Stack

- **Runtime**: .NET 8
- **Web Framework**: ASP.NET Core 8 (REST API + MVC Razor)
- **Database**: SQL Server (LocalDB)
- **ORM**: Entity Framework Core 8
- **Authentication**: JWT Bearer tokens
- **Identity**: ASP.NET Core Identity
- **Testing**: xUnit + WebApplicationFactory integration tests

## Architecture

The solution follows Clean Architecture with four projects:

| Project | Responsibility |
|---------|---------------|
| **Domain** | Entities, enums, custom exceptions. No framework dependencies. |
| **Application** | Use-case services, DTOs, interface contracts. No EF Core references. |
| **Infrastructure** | EF Core DbContext, Identity, JWT token service, data seeding. |
| **API** | Controllers, middleware, Swagger, Razor dashboard, DI composition root. |

Dependencies point inward: API → Infrastructure → Application → Domain.

## Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server LocalDB (included with Visual Studio)

### Run
```bash
dotnet restore
dotnet run --project API --launch-profile https
```

The application will:
1. Create the database automatically via EF Core migrations
2. Seed deterministic development data (5 users, 3 tickets)
3. Start listening on `https://localhost:7060`

### Access Points
- **Swagger UI**: `https://localhost:7060/swagger`
- **Staff Dashboard**: `https://localhost:7060/support/dashboard` (login as agent first via Swagger)

### Test Accounts

| Email | Password | Role |
|-------|----------|------|
| customer1@demo.local | Demo!Customer1 | Customer |
| customer2@demo.local | Demo!Customer2 | Customer |
| agent1@demo.local | Demo!Agent1 | SupportAgent |
| agent2@demo.local | Demo!Agent2 | SupportAgent |
| admin@demo.local | Demo!Admin1 | Admin |

### Run Tests
```bash
dotnet test Tests/Tests.csproj --verbosity normal
```
All 24 integration tests cover acceptance scenarios A–L from the challenge specification.

## API Endpoints

| Method | Route | Who | Purpose |
|--------|-------|-----|---------|
| POST | /api/auth/login | Anonymous | Login and receive JWT |
| POST | /api/auth/register | Anonymous | Register as customer |
| POST | /api/auth/forgot-password | Anonymous | Request password reset token |
| POST | /api/auth/reset-password | Anonymous | Reset password with token |
| POST | /api/tickets | Customer | Create a ticket |
| GET | /api/tickets | Authorized | List/filter/page tickets |
| GET | /api/tickets/{ticketNumber} | Authorized | Get ticket with messages |
| POST | /api/tickets/{ticketNumber}/claim | Agent/Admin | Claim an Open ticket |
| POST | /api/tickets/{ticketNumber}/resolve | Assigned Agent/Admin | Resolve InProgress ticket |
| POST | /api/tickets/{ticketNumber}/reopen | Owner Customer | Reopen within 48 hours |
| POST | /api/tickets/{ticketNumber}/close | Owner/Admin | Close a Resolved ticket |
| POST | /api/tickets/{ticketNumber}/messages | Authorized | Add message/note |
| GET | /api/tickets/{ticketNumber}/history | Agent/Admin | View audit history |
| GET | /support/dashboard | Agent/Admin | MVC staff dashboard |

## Key Features

- **Workflow State Machine**: Open → InProgress → Resolved → Closed (with Resolved → InProgress via reopen)
- **Optimistic Concurrency**: SQL Server `rowversion` prevents two agents from claiming the same ticket
- **Idempotent Claim**: Same agent re-claiming returns 200 without adding duplicate history
- **48-Hour Reopen Window**: Testable via `TimeProvider` abstraction
- **Internal Notes**: Only visible to staff; filtered at query boundary for customers
- **Ownership Privacy**: Customers get 404 (not 403) for other customers' tickets
- **Atomic Audit Trail**: Every state change writes a `TicketHistory` row in the same database transaction
- **Consistent Error Contract**: `{ code, message, correlationId }` on every error response

## Project Structure

```
TicketSupportSystem/
├── Domain/                     # Core entities, enums, exceptions
│   ├── Entities/               # Ticket, TicketMessage, TicketHistory, ApplicationUser
│   ├── Enums/                  # TicketStatus, TicketPriority, EventType
│   └── Exceptions/             # TicketDomainException hierarchy
├── Application/                # Business logic layer
│   ├── DTOs/                   # Request/response models
│   ├── Interfaces/             # IAppDbContext, ITicketService, ICurrentUserService, ITokenService
│   └── Services/               # TicketService (workflow logic)
├── Infrastructure/             # Implementation layer
│   ├── Data/                   # AppDbContext, EF Core configurations
│   ├── Migrations/             # EF Core migrations
│   ├── Seeding/                # DataSeeder (deterministic dev data)
│   └── Services/               # TokenService, CurrentUserService
├── API/                        # Presentation layer
│   ├── Controllers/            # AuthController, TicketsController, SupportController
│   ├── Middleware/              # ExceptionMiddleware, CorrelationIdMiddleware
│   ├── Views/Support/          # Dashboard.cshtml (Razor)
│   └── Program.cs              # Composition root
└── Tests/                      # Integration tests
    ├── AuthTests.cs            # Scenarios A, B
    ├── TicketTests.cs          # Scenarios C, D, E, K
    ├── ClaimTests.cs           # Scenarios F, G
    ├── WorkflowTests.cs        # Scenarios H, I
    ├── MessageTests.cs         # Scenario J
    └── DashboardTests.cs       # Scenario L
```

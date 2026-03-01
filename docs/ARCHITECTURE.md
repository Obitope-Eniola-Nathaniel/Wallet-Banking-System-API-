# Architecture — Wallet Banking API

This document explains **how** the solution is structured and **why** these choices were made. It is written so recruiters and senior developers can see that the architecture is intentional and production-ready.

---

## 1. High-Level Layers

```
┌─────────────────────────────────────────────────────────────┐
│  Api (Presentation)                                         │  Controllers, Middleware, DI composition
│  - Sends Commands/Queries via MediatR                       │
│  - Never calls Repository or Domain directly                │
├─────────────────────────────────────────────────────────────┤
│  Application (Use Cases / CQRS)                              │  Commands, Queries, Handlers
│  - Depends ONLY on Domain (IWalletRepository)              │
│  - One handler per command/query                             │
├─────────────────────────────────────────────────────────────┤
│  Domain (Core)                                               │  Entities, DTOs, Repository interfaces
│  - ZERO dependencies on other projects or packages           │
│  - Pure C#; no EF, no MediatR, no MongoDB                    │
├─────────────────────────────────────────────────────────────┤
│  Infrastructure (Data Access)                                │  Implements IWalletRepository
│  - Depends ONLY on Domain                                    │  PostgreSQL, Dapper
└─────────────────────────────────────────────────────────────┘
```

**Dependency rule:** Dependencies point **inward**. Domain does not depend on anything. Application does not depend on Infrastructure. The Api project is the **composition root**: it references both Application and Infrastructure and wires `IWalletRepository` → `WalletRepository`.

---

## 2. Why the Repository Interface Lives in Domain

In many tutorials, the repository interface is placed in Infrastructure. In **Clean Architecture** (Uncle Bob) and in production codebases:

- The **application layer** (use cases) needs to depend on an **abstraction** (e.g. “I need to save a wallet”) without knowing whether that’s PostgreSQL, MongoDB, or a test double.
- So the **interface** is defined in **Domain** (or in an Application contracts project that Domain can also reference). **Infrastructure** implements it.

Consequences:

- **Application** never references **Infrastructure**. So you can unit-test handlers by passing a fake `IWalletRepository` without touching the database.
- You can swap the database (e.g. add MongoDB in Phase 5) by adding another implementation of `IWalletRepository` and registering it in the composition root. No change to Application or Domain.

**In this repo:** `WalletBankingAPI.Domain` contains `IWalletRepository`; `WalletBankingAPI.Infrastructure` contains `WalletRepository`.

---

## 3. Why Domain Has No External Dependencies

The Domain project has **no** NuGet references (no EF Core, no MongoDB.Bson, no MediatR). Reasons:

- **Testability:** Domain logic can be unit-tested without loading any infrastructure.
- **Stability:** Upgrading EF Core or changing the database does not force Domain to change.
- **Clarity:** Domain models are “plain” C# (e.g. `Wallet` with `decimal Balance`). Persistence concerns (mapping, keys, indexes) belong in Infrastructure. If you later add EF Core, the **entity that maps to the table** can live in Infrastructure and map to/from a Domain `Wallet` if you prefer to keep Domain persistence-ignorant.

Using **decimal** for money (Balance, amounts) is the industry standard in C# to avoid floating-point rounding errors.

---

## 4. CQRS in This Project

- **Commands** (write): `CreateWalletCommand`, `UpdateWalletCommand`, `DeleteWalletCommand`. Each has one handler; handlers call `IWalletRepository` and return `ApiResponse`.
- **Queries** (read): `GetWalletByIdQuery`, `GetAllWalletsQuery`. Handlers read via `IWalletRepository` and return `ApiResponse` with `Data` set.

Controllers only **send** commands/queries via `IMediator.Send(...)`. They do not instantiate repositories or domain entities. This keeps the API layer thin and makes it obvious where business logic lives (Application + Domain).

---

## 5. Phase 1 Request Flow (Example: Create Wallet)

1. **HTTP** `POST /api/wallet` with body `{ "ownerName": "Jane", "initialBalance": 100 }`.
2. **WalletController** receives the request, builds `CreateWalletCommand`, calls `await mediator.Send(command)`.
3. **MediatR** finds `CreateWalletCommandHandler`, invokes it with the command.
4. **CreateWalletCommandHandler** validates (e.g. initialBalance >= 0), then calls `await walletRepository.CreateAsync(request)`.
5. **WalletRepository** (Infrastructure) opens a connection to PostgreSQL, inserts into `"Wallet"`, returns `ApiResponse`.
6. The response bubbles back: Handler → MediatR → Controller → HTTP response.

No repository or SQL is visible in the Controller or in the Application layer beyond the interface.

---

## 6. Error Handling and Consistency

- **GlobalExceptionMiddleware** catches unhandled exceptions and returns a single JSON shape (e.g. `ApiResponse` with `IsSuccessful = false`, `Message`, `ResponseCode`). This matches the PRD requirement for standardized error responses.
- Handlers and repository return `ApiResponse` for commands and for query results, so the API contract is consistent (success/failure, message, data).

---

## 7. What Makes This “Production-Ready”

- **Clear boundaries:** Domain, Application, Infrastructure, Api with a strict dependency rule.
- **Testability:** Handlers can be tested with a fake `IWalletRepository`; Domain has no infrastructure.
- **Consistent responses:** `ApiResponse` and global exception handling.
- **Soft delete:** Optional but recommended for audit; implemented as `IsDeleted` and filtered in queries.
- **Decimal for money:** Avoids rounding issues.
- **Documentation:** PRD, ARCHITECTURE, LEARNING, and code comments explain intent for future you and for reviewers.

---

## 8. Folder and Project Layout (Phase 1)

```
WalletBankingAPI/
├── PRD.md, README.md, INTERVIEW_QA.md, ARCHITECTURE.md, LEARNING.md
├── database/
│   └── 001_CreateWalletTable.sql
└── src/
    ├── WalletBankingAPI.sln
    ├── WalletBankingAPI.Domain/          (entities, DTOs, IWalletRepository)
    ├── WalletBankingAPI.Application/    (commands, queries, handlers)
    ├── WalletBankingAPI.Infrastructure/  (WalletRepository, SQL)
    └── WalletBankingAPI.Api/            (controllers, middleware, Program.cs)
```

This layout is scalable: Phase 2 (Deposit/Withdraw) adds new commands and handlers; Phase 3 adds a `Transaction` entity and repository; Phase 4 adds transfer and concurrency; Phase 5 can add another implementation of a read-side or event publishing without breaking existing layers.

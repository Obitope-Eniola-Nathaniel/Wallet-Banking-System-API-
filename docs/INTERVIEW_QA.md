# 🎤 Real-Life Interview Questions & Answers

**Project:** Wallet / Banking System API  
**Use this doc to:** Prepare for backend/architecture interviews and to understand *why* we make design decisions in this project.

Each section has **questions** you might hear in real interviews and **answers** with short explanations. Use the "Learning note" and "In this project" callouts to connect answers to our codebase.

---

## Table of Contents

1. [Clean Architecture](#1-clean-architecture)
2. [CQRS](#2-cqrs)
3. [MediatR](#3-mediatr)
4. [Repository Pattern](#4-repository-pattern)
5. [Wallet / Banking Domain](#5-wallet--banking-domain)
6. [Concurrency & Transactions](#6-concurrency--transactions)
7. [API & Security](#7-api--security)
8. [Testing & Maintainability](#8-testing--maintainability)

---

## 1. Clean Architecture

### Q1. What is Clean Architecture, and why use it?

**Answer:**  
Clean Architecture (Uncle Bob) is a way to structure code so that **business logic does not depend on frameworks, UI, or databases**. Dependencies point **inward**: outer layers (API, Infrastructure) depend on inner layers (Application, Domain). The **Domain** has no dependencies; it only contains entities and business rules.

**Why use it:**  
- **Testability** — You can test business logic without a real DB or HTTP server.  
- **Maintainability** — You can swap database or UI without rewriting core logic.  
- **Clear boundaries** — Everyone knows where to put new code (controllers vs handlers vs repositories).

**Learning note:** In our Wallet API, Domain holds `Wallet`, `Transaction`, and interfaces; Infrastructure implements repositories; Application contains use cases (handlers).

---

### Q2. Explain the Dependency Rule. Who can depend on whom?

**Answer:**  
The **Dependency Rule** says: *source code dependencies can only point inward*.  

- **Presentation (API)** → can depend on Application (and Domain indirectly).  
- **Application** → can depend on Domain (and on abstractions like `IWalletRepository`).  
- **Domain** → must not depend on anything (no project references to Infrastructure, API, or MediatR).  
- **Infrastructure** → can depend on Domain (implements interfaces defined in Domain).

So: **Domain is the center**. Application and Infrastructure both depend on Domain; they do not depend on each other. The API/composition root wires Infrastructure implementations to the interfaces Application uses.

**In this project:** Controllers call MediatR; handlers use `IWalletRepository` (interface in Domain); Infrastructure implements `WalletRepository`. Application must **not** reference Infrastructure project—only Domain.

---

### Q3. Where do you put business rules: Domain vs Application?

**Answer:**  
- **Domain:** Invariants and rules that are always true for the entity (e.g. "balance cannot be negative", "amount must be positive"). These can live in the entity (e.g. `Wallet.Withdraw(amount)` that checks balance) or in small domain services.  
- **Application (use cases):** Orchestration and workflow (e.g. "when user deposits, create a Transaction record and update Wallet balance"). Application calls Domain to enforce rules and calls repositories to persist.

**Rule of thumb:** If it’s a rule that would exist even without an API or database, it belongs in Domain. If it’s "when X happens, do steps A, B, C," it’s Application.

**In this project:** "Cannot withdraw more than balance" is Domain (or validated in Application using Domain). "Create wallet then emit WalletCreated event" is Application.

---

## 2. CQRS

### Q4. What is CQRS? Why separate reads and writes?

**Answer:**  
**CQRS** = Command Query Responsibility Segregation. It means we **separate**:

- **Commands** — operations that **change state** (Create Wallet, Deposit, Withdraw, Transfer). They return minimal data (e.g. success/failure, new ID).  
- **Queries** — operations that **read state** (Get Wallet, List Wallets, Get Transaction History). They return data and do not change state.

**Why separate:**  
- Different scaling (e.g. more reads than writes; you can optimize read models separately).  
- Clear intent in code (this handler changes state, this one only reads).  
- Easier to add read-side optimizations (caches, denormalized tables) without touching write logic.

**Learning note:** In our project, "CreateWallet" is a Command; "GetWalletById" and "GetTransactionHistory" are Queries. Both are handled via MediatR but in different “sides” of CQRS.

---

### Q5. When would you *not* use CQRS?

**Answer:**  
- **Very simple CRUD** with no complex reads or scaling needs — CQRS adds structure that might be overkill.  
- **Tight deadlines** for a one-off app — the extra commands/queries/handlers can slow initial delivery.  
- **Team not familiar** with the pattern — can lead to confusion unless you document and follow conventions.

CQRS shines when you have complex read models, high read load, or need clear audit trails (e.g. every change as a command). Our Wallet/Banking API benefits because we have transactions, history, and future reporting.

---

### Q6. How do Commands and Queries differ in terms of return values and side effects?

**Answer:**  
- **Commands:** Change state (create/update/delete). Return type is often a result DTO (e.g. `CommandResult` with Id, success, message). They should **not** return full entities unless necessary (to avoid coupling and over-fetching).  
- **Queries:** Do not change state. Return DTOs or view models (e.g. `WalletDto`, `TransactionHistoryDto`). Can be cached safely.

**In this project:** `CreateWalletCommand` returns something like `WalletCreatedResult`; `GetWalletByIdQuery` returns `WalletDto` or similar.

---

## 3. MediatR

### Q7. What is MediatR, and what problem does it solve?

**Answer:**  
MediatR is a **mediator pattern** implementation for .NET. Controllers (or any client) **send a request** (command or query) to the mediator; the mediator finds the **single handler** for that request and invokes it. The controller does not know about handlers or repositories.

**Problems it solves:**  
- **Thin controllers** — No business logic or repository calls in controllers.  
- **Single responsibility** — One handler per command/query.  
- **Testability** — You can test handlers in isolation by mocking the repository.  
- **Decoupling** — Adding a new use case = new command/query + handler; no change to existing controllers.

**In this project:** Controller calls `_mediator.Send(new GetWalletByIdQuery(id))`; `GetWalletByIdHandler` runs and uses `IWalletRepository`. Controller never touches the repository.

---

### Q8. How does MediatR find the right handler?

**Answer:**  
MediatR uses **assembly scanning**. When you register `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`, it finds all types that implement `IRequestHandler<TRequest, TResponse>`. When you `Send(request)`, MediatR looks up the handler for the **exact request type** (e.g. `GetWalletByIdQuery`) and invokes `Handle(request, cancellationToken)`.

So: one request type → one handler. No manual if/switch; the type system and MediatR do the routing.

---

### Q9. What are pipelines/behaviors in MediatR? When would you use them?

**Answer:**  
**Behaviors** are like middleware for MediatR. They wrap the execution of every handler. You implement `IPipelineBehavior<TRequest, TResponse>` and get `RequestHandlerDelegate<TResponse> next()`. You can run logic **before** and **after** the handler (e.g. logging, validation, transaction scope).

**Use cases:**  
- **Validation** — Validate the request before it reaches the handler (e.g. FluentValidation).  
- **Logging** — Log every command/query and duration.  
- **Transactions** — Open a DB transaction before the handler and commit/rollback after.  
- **Unit of Work** — Ensure one transaction per command.

**In this project:** You could add a `ValidationBehavior` and a `TransactionBehavior` so every command runs inside a DB transaction without repeating that code in each handler.

---

## 4. Repository Pattern

### Q10. What is the Repository pattern? Why not use DbContext directly in handlers?

**Answer:**  
The **Repository** is an abstraction over data access. Handlers depend on `IWalletRepository` (interface), not on EF Core `DbContext` or Npgsql. Infrastructure provides the concrete `WalletRepository` that talks to the database.

**Why not DbContext in handlers:**  
- **Dependency Rule** — Application layer should not reference EF Core or Npgsql (those are Infrastructure).  
- **Testability** — In tests you can replace `IWalletRepository` with a fake in-memory implementation.  
- **Flexibility** — You can switch to Dapper, MongoDB, or a mix without changing handlers.

**In this project:** Domain (or Application) defines `IWalletRepository`; Infrastructure implements it with PostgreSQL (and later optionally MongoDB).

---

### Q11. Should the repository return Domain entities or DTOs?

**Answer:**  
**For write side (commands):** Repository usually accepts and returns **Domain entities** (or DTOs that map to entities inside the repository). The handler works with domain objects.  
**For read side (queries):** Repository can return **read-model DTOs** directly (e.g. `WalletDto`, `TransactionHistoryItem`) to avoid loading full entities when you only need a view. This is CQRS: write model = entities; read model = DTOs/views.

**In this project:** Create/Update might use `Wallet` entity; GetWalletById and ListWallets might return `WalletDto` or similar from the repository or a dedicated query handler that maps from entity to DTO.

---

### Q12. Generic repository vs specific repository (e.g. IWalletRepository)?

**Answer:**  
- **Generic** `IRepository<T>`: One interface for all entities (GetById, Add, Update, Delete). Good for simple CRUD with little domain logic.  
- **Specific** `IWalletRepository`, `ITransactionRepository`: Methods match use cases (e.g. `GetByOwnerName`, `GetTransactionHistory(walletId, page, size)`). Better when each aggregate has different access patterns.

For a **Wallet/Banking API**, specific repositories are usually better: wallet and transaction have different rules and query needs. Our PRD uses `Wallet` and `Transaction` with specific operations, so prefer **IWalletRepository** and **ITransactionRepository**.

---

## 5. Wallet / Banking Domain

### Q13. Why use `decimal` for money instead of `double` or `float`?

**Answer:**  
**decimal** is decimal floating-point; **double/float** are binary. Binary types cannot represent many decimal fractions exactly (e.g. 0.1), so you get rounding errors when adding many amounts. For **money**, we need exact decimal arithmetic (e.g. 10.01 + 20.02 = 30.03). So always use **decimal** for currency amounts in C# and in the database (e.g. PostgreSQL `numeric`/`decimal`).

**In this project:** `Wallet.Balance` and `Transaction.Amount` are `decimal`.

---

### Q14. How would you prevent double-spending or race conditions when withdrawing or transferring?

**Answer:**  
- **Database transaction:** Wrap "read balance → check → update balance" in a single **transaction** so the read and write are atomic.  
- **Optimistic locking:** Add a `Version` (or `RowVersion`) column; on update, check that version hasn’t changed. If it has, retry or return a concurrency error.  
- **Pessimistic locking:** `SELECT ... FOR UPDATE` so the row is locked until the transaction commits (other transactions wait).  
- **Idempotency:** For transfers, use a unique idempotency key (e.g. client-generated) so the same request applied twice doesn’t double-move money.

**In this project:** Phase 4 explicitly asks for concurrency control; use transactions + optional version column or `FOR UPDATE` for critical paths (withdraw, transfer).

---

### Q15. How would you design a "Transfer between two wallets" use case?

**Answer:**  
- **Command:** `TransferCommand` with FromWalletId, ToWalletId, Amount, optional IdempotencyKey.  
- **Handler:**  
  1. Validate (amount > 0, From ≠ To, wallets exist).  
  2. Open a **database transaction**.  
  3. Debit From wallet (with balance check and locking).  
  4. Credit To wallet.  
  5. Create two **Transaction** records (e.g. Type=TransferOut and Type=TransferIn) or one Transfer record linking both wallets.  
  6. Commit transaction.  
- **Idempotency:** If the client sends the same IdempotencyKey again, return the same result without applying the transfer twice.

**In this project:** This is Phase 4; the same pattern applies: one command, one handler, one transaction, optional domain events (WalletDebited, WalletCredited) for Phase 5.

---

## 6. Concurrency & Transactions

### Q16. What is the difference between a unit of work and a repository?

**Answer:**  
- **Repository:** Abstracts **one** entity or aggregate (e.g. Wallet). Methods like Add, GetById, Update.  
- **Unit of Work:** Groups **multiple** repository operations into **one transaction**. You call `WalletRepository.Update(...)` and `TransactionRepository.Add(...)`, then `UnitOfWork.Commit()`. Either all persist or none.

In many apps, **DbContext** (EF Core) acts as both: it’s the unit of work (SaveChanges = commit), and DbSet<T> are like repositories. In our project, we might have one transaction per MediatR command (e.g. via a behavior) and multiple repository calls inside that transaction.

---

### Q17. When would you use optimistic vs pessimistic locking?

**Answer:**  
- **Optimistic:** Assume conflicts are rare. Read version, do work, update with "WHERE version = @oldVersion". If no row updated, someone else changed it → retry or return conflict. Good for lower contention, better throughput.  
- **Pessimistic:** Lock rows (e.g. `SELECT FOR UPDATE`) so others wait. Good when conflicts are frequent (e.g. same wallet updated by many requests). Higher consistency, lower concurrency.

For **wallet balance**, high contention might suggest pessimistic locking or at least a transaction with `FOR UPDATE` on the wallet row during debit/credit. For general "update profile" low contention, optimistic is often enough.

---

## 7. API & Security

### Q18. How would you secure the Wallet API? (API Key vs JWT)

**Answer:**  
- **API Key:** Simple: client sends a key in header (e.g. `X-Api-Key`). Server validates it (e.g. from config or DB). Good for server-to-server or internal tools. **In this project:** PRD mentions API keys; we can add `ApiKeyMiddleware` that checks the key before reaching controllers.  
- **JWT:** For user-based auth: user logs in, gets a signed token; each request sends `Authorization: Bearer <token>`. Good for multiple users and fine-grained claims (e.g. userId for "my wallets only"). PRD says "JWT later" — so start with API key, add JWT when you need per-user identity.

---

### Q19. How would you standardize error responses?

**Answer:**  
Use a **global exception middleware** (or exception filter). Catch exceptions, map them to HTTP status and a consistent JSON shape, e.g.:

```json
{
  "success": false,
  "errorCode": "INSUFFICIENT_BALANCE",
  "message": "Cannot withdraw more than current balance.",
  "traceId": "..."
}
```

Business rule violations (e.g. insufficient balance) → 400 or 422; not found → 404; concurrency conflict → 409. Never leak stack traces or internal details in production. **In this project:** PRD asks for `GlobalExceptionMiddleware`; our existing CQRS project has similar middleware you can reuse or adapt.

---

## 8. Testing & Maintainability

### Q20. How would you unit test a "Withdraw" command handler?

**Answer:**  
1. Create a **fake** or **mock** `IWalletRepository` (e.g. returns a wallet with balance 100).  
2. Instantiate `WithdrawCommandHandler` with that fake repository.  
3. Call `handler.Handle(new WithdrawCommand { WalletId = id, Amount = 30 }, default)`.  
4. Assert: repository’s Update was called with wallet balance = 70; or assert on returned result.  
5. **Negative test:** Wallet with balance 10, withdraw 20 → expect validation/concurrency error and that Update was not called (or called with correct failure path).

No real database; fast, deterministic tests. Integration tests would use a real DB (or Testcontainers) to test the full stack.

---

### Q21. What makes this Wallet API "maintainable" from an architecture perspective?

**Answer:**  
- **Clear layers** — New feature = new command/query + handler + maybe repository method; no need to touch unrelated layers.  
- **Interfaces in Domain** — Swapping PostgreSQL for another store only touches Infrastructure.  
- **One handler per use case** — Easy to find and change behavior.  
- **Documentation** — PRD + this Q&A + README help new developers (or you in 6 months) understand and take over the project.

**In this project:** We keep PRD, INTERVIEW_QA, and README in the `WalletBankingAPI` folder so you can hand over or restart the project with full context.

---

## Quick Reference: Terms Used in This Doc

| Term | Short meaning |
|------|----------------|
| **Clean Architecture** | Layered design with Dependency Rule; Domain at center. |
| **CQRS** | Separate Commands (write) and Queries (read). |
| **MediatR** | Mediator library; request → single handler. |
| **Repository** | Abstraction over data access (interface in Domain, impl in Infrastructure). |
| **Unit of Work** | Groups multiple repo operations in one transaction. |
| **Optimistic locking** | Version column; detect conflicts on update. |
| **Pessimistic locking** | Lock row (e.g. FOR UPDATE) during transaction. |
| **Idempotency** | Same request applied twice = same effect as once. |

---

*Use this document alongside the PRD and README when implementing or handing over the Wallet Banking API. Good luck in interviews and with the project.*

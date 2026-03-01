# 🏦 Product Requirements Document (PRD)

**Project Name:** Wallet / Banking System API  
**Owner:** You / OEN Tech  
**Target Users:** Developers for learning; eventually internal or real-world users  
**Date:** 28-Feb-2026  

---

## 📋 Document Purpose

This PRD defines the scope, features, and technical direction for the Wallet/Banking API. Use it as the single source of truth when implementing or handing over the project. Each phase is designed to grow skills from **beginner → senior** backend engineer.

---

## 1️⃣ Purpose & Goals

### Purpose

Build a **Wallet / Banking API** that supports:

- Wallet creation
- Money transactions (deposit, withdraw)
- Transfer between wallets

The system will be designed using **Clean Architecture + CQRS** principles so you can grow from beginner to senior backend skills within the same codebase.

### Goals

| Goal | Description |
|------|-------------|
| **Learning platform** | Provide a codebase that demonstrates enterprise-grade backend architecture |
| **Full CRUD** | Perform full CRUD operations for **Wallet** and **Transaction** entities |
| **Business rules** | Enforce financial safety rules (e.g. no negative balance, positive amounts) |
| **Future scaling** | Enable multi-database support, caching, and event-driven features later |

---

## 2️⃣ Phased Features

### Phase 1 — Beginner (CRUD Wallets)

| Feature | Description | Entities |
|---------|-------------|----------|
| **Create Wallet** | Add a new wallet with owner name & initial balance | Wallet |
| **Update Wallet** | Update wallet owner info | Wallet |
| **Delete Wallet** | Remove wallet (soft delete recommended) | Wallet |
| **Get Wallet** | Retrieve wallet info by ID | Wallet |
| **List Wallets** | Retrieve all wallets | Wallet |

**Learning focus:** Layered structure, MediatR commands/queries, repository pattern, DI.

---

### Phase 2 — Junior (Deposit / Withdraw with Rules)

| Feature | Description | Business Rules |
|---------|-------------|----------------|
| **Deposit Money** | Add funds to a wallet | Amount > 0 |
| **Withdraw Money** | Remove funds from wallet | Cannot exceed balance, Amount > 0 |
| **Balance Check** | Return current balance | Must be accurate after transactions |

**Learning focus:** Domain validation, invariants, and use-case handlers.

---

### Phase 3 — Mid-Level (Transactions & History)

| Feature | Description |
|---------|-------------|
| **Transaction Log** | Record each deposit/withdraw as a **Transaction** entity |
| **Transaction History** | List transactions per wallet |
| **CQRS Separation** | Read and write operations clearly separated (queries vs commands) |

**Learning focus:** CQRS in practice, read vs write models, audit trail.

---

### Phase 4 — Upper Mid-Level (Transfers & Concurrency)

| Feature | Description |
|---------|-------------|
| **Transfer Between Wallets** | Move money from one wallet to another **atomically** |
| **Concurrency Control** | Prevent race conditions / double spending (e.g. optimistic locking, transactions) |
| **Pagination / Filtering** | For transaction history queries |

**Learning focus:** Database transactions, concurrency, idempotency, pagination DTOs.

---

### Phase 5 — Senior-Level (Advanced Architecture)

| Feature | Description |
|---------|-------------|
| **Multi-Database Support** | MongoDB + PostgreSQL or others |
| **Caching** | Redis for balance reads |
| **Middleware** | API Key, encryption, logging, exception handling |
| **Event-Driven** | Domain events (e.g. WalletCredited, WalletDebited) |
| **Audit Logs** | Track every critical operation |
| **Rate Limiting** | Protect API endpoints |

**Learning focus:** Cross-cutting concerns, event sourcing readiness, resilience.

---

## 3️⃣ Entities

### Wallet

| Property | Type | Notes |
|----------|------|--------|
| **Id** | GUID | Primary key |
| **OwnerName** | string | Wallet owner identifier/name |
| **Balance** | decimal | Current balance (use decimal for money) |
| **CreatedAt** | datetime | Creation timestamp |
| **UpdatedAt** | datetime | Last update timestamp |
| **IsDeleted** | bool | Optional; for soft delete |

### Transaction

| Property | Type | Notes |
|----------|------|--------|
| **Id** | GUID | Primary key |
| **WalletId** | GUID (FK) | Reference to Wallet |
| **Type** | enum | Deposit / Withdraw / Transfer |
| **Amount** | decimal | Transaction amount (always positive; direction from Type) |
| **Timestamp** | datetime | When the transaction occurred |
| **Description** | string (optional) | Free-text note or reference |

**Note:** For **Transfer**, consider a link to a second transaction (e.g. `RelatedTransactionId`) or a single “Transfer” record with FromWalletId/ToWalletId depending on your design.

---

## 4️⃣ Technical Requirements

| Area | Requirement |
|------|-------------|
| **Framework** | ASP.NET Core (API) |
| **Pattern** | Clean Architecture |
| **Command/Query** | MediatR (CQRS) |
| **Repository** | Repository pattern (interface in Domain, implementation in Infrastructure) |
| **Database** | Start with **PostgreSQL** (MongoDB can be added later) |
| **Dependency Injection** | ASP.NET Core built-in DI |
| **Unit Testing** | xUnit or NUnit |

---

## 5️⃣ Architecture Guidelines

### Layering (Dependency Rule)

```
Presentation (API / Controllers)
    ↓ depends on
Application (Commands / Queries / Handlers)
    ↓ depends on
Domain (Entities, Interfaces, Business Rules)
    ↑ implemented by
Infrastructure (Repository Implementation, Database, External Services)
```

### Rules

- **Controllers** never call the repository directly; they send commands/queries via MediatR.
- **Business rules** reside only in **Domain** (or in Application use-case logic that uses Domain).
- **Infrastructure** implements repository interfaces and database logic only.
- **Commands & Queries** are handled via MediatR; one handler per command/query type.

---

## 6️⃣ Non-Functional Requirements

- **Testable & maintainable** — Clear boundaries so unit and integration tests are straightforward.
- **Standardized errors** — Use something like `GlobalExceptionMiddleware` and a consistent error response shape.
- **Security** — Secure API endpoints (e.g. API keys now; JWT later if needed).
- **Modular & scalable** — Easy to add new features (e.g. new commands/queries, new repositories) without breaking existing code.

---

## 7️⃣ Constraints / Notes

- Start simple and **phase in** complexity (Phase 1 → 2 → 3 → 4 → 5).
- Use **DTOs** to separate API input/output from Domain entities.
- **Avoid static database context**; prefer DI for all data access.
- Follow an **enterprise-level folder structure** (see existing CQRS/Kinzmah project for reference).
- **Future plan:** multi-database, caching, background jobs — keep these in mind when naming and structuring.

---

## ✅ Next Steps (Recommended Order)

1. **Phase 1:** Build CRUD for Wallets (Create, Read, Update, Delete, List).
2. Implement **MediatR** + Commands / Queries for each operation.
3. Add **Repository pattern** + DI (interface in Domain, implementation in Infrastructure).
4. **Phase 2:** Add Deposit / Withdraw with validation and balance checks.
5. **Phase 3:** Introduce Transaction entity and history queries.
6. **Phase 4:** Add Transfer and concurrency control; add pagination/filtering.
7. **Phase 5:** Add multi-database, caching, middleware, events, audit, rate limiting as needed.
8. Refactor as complexity increases; keep PRD and architecture docs updated.

---

*End of PRD. For interview Q&A and handover notes, see `INTERVIEW_QA.md` and `README.md` in this folder.*

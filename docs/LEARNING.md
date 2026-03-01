# Learning Progress & Portfolio Talking Points

Use this document to **track what you’ve learned** in each phase and to **prepare for interviews and GitHub reviews**. Senior devs and recruiters often look for “why” and “what you’d do next,” not only “what you built.”

---

## Phase 1 — What You Built and What It Proves

### Delivered (CRUD Wallets)

- **Create Wallet** — `POST /api/wallet` with `ownerName`, `initialBalance`.
- **Update Wallet** — `PUT /api/wallet/{id}` with `ownerName`.
- **Delete Wallet** — `DELETE /api/wallet/{id}` (soft delete).
- **Get Wallet** — `GET /api/wallet/{id}`.
- **List Wallets** — `GET /api/wallet`.

All via **MediatR** (commands/queries), **Clean Architecture** (Domain ← Application, Domain ← Infrastructure, Api as composition root), and **repository pattern** (`IWalletRepository` in Domain, implementation in Infrastructure).

### What You Can Say in Interviews

1. **“I followed Clean Architecture.”**  
   - Domain has no dependencies; Application depends only on Domain (repository interface); Infrastructure implements the interface. Api composes everything.

2. **“I used CQRS with MediatR.”**  
   - Writes (Create, Update, Delete) are Commands; reads (Get by Id, Get All) are Queries. One handler per command/query. Controllers only send requests to MediatR.

3. **“I kept the repository interface in the Domain layer.”**  
   - So the application layer doesn’t depend on Infrastructure. That makes unit testing and swapping databases easier.

4. **“I used decimal for money.”**  
   - Avoids floating-point rounding; industry standard for currency in C#.

5. **“I standardized API responses and errors.”**  
   - `ApiResponse` for success/failure; `GlobalExceptionMiddleware` for unhandled exceptions so clients always get a consistent shape.

6. **“I used soft delete for wallets.”**  
   - Better for audit and compliance; we don’t lose history.

### What You Learned (Concepts)

- **Dependency Rule** — Who can reference whom (Domain → nothing; Application → Domain; Infrastructure → Domain; Api → Application + Infrastructure).
- **CQRS** — Separating commands (write) and queries (read); each use case has a single handler.
- **Repository pattern** — Abstract data access behind an interface; concrete implementation in Infrastructure.
- **Composition root** — The Api project is the only place that ties Infrastructure implementations to interfaces (DI registration).
- **Thin controllers** — No business logic or repository calls; only MediatR and HTTP.

### What You’d Do Next (Shows Growth Mindset)

- **Phase 2:** Deposit/Withdraw with validation (amount > 0, balance >= 0).  
- **Phase 3:** `Transaction` entity and transaction history (read model).  
- **Phase 4:** Transfer between wallets (atomic), concurrency control, pagination.  
- **Phase 5:** Multi-database, caching, domain events, audit logs, rate limiting.  
- **Testing:** Unit tests for handlers with a fake `IWalletRepository`; integration tests with a test database (e.g. Testcontainers).  
- **Validation:** FluentValidation in a MediatR pipeline behavior so invalid commands never reach the handler.

---

## How to Use This on GitHub

1. **README** — Short project description, link to PRD, “Phase 1: CRUD Wallets,” how to run (see GETTING_STARTED or README in `src`).
2. **ARCHITECTURE.md** — Link from README; shows you understand layers and dependency rule.
3. **LEARNING.md** (this file) — Shows you reflect on what you built and how you’d extend it.
4. **Commits** — Use clear messages: “feat: add CreateWallet command and handler,” “docs: add ARCHITECTURE.md.”
5. **PR description** (if you use PRs for your own branches) — “Implements Phase 1 CRUD per PRD; follows Clean Architecture and CQRS.”

Senior devs will look for: consistent structure, separation of concerns, and your ability to explain and extend the design. This repo is set up to support that narrative.

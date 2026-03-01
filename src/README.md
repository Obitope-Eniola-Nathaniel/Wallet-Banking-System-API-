# Wallet Banking API

A small API to create, read, update, and delete wallets. Built with .NET 9, Clean Architecture, and CQRS (MediatR).

---

## Run the project

1. **Database** — Create a PostgreSQL database (e.g. `WalletBanking`) and run the script:

   ```
   database/001_CreateWalletTable.sql
   ```

2. **Connection string** — In `src/WalletBankingAPI.Api/appsettings.Development.json`, set:

   ```json
   "ConnectionStrings": {
     "PostgreSQL": "Host=localhost;Port=5432;Database=WalletBanking;Username=postgres;Password=YOUR_PASSWORD;"
   }
   ```

3. **Start the API** — In a terminal, from the `src` folder:
   ```bash
   dotnet run --project WalletBankingAPI.Api
   ```
   Then open the URL shown (e.g. http://localhost:5000/swagger) and try the endpoints.

---

## Understand how it works

**Read [START_HERE.md](./START_HERE.md).**  
It explains, in simple terms:

- What each of the 4 layers does (Api, Application, Domain, Infrastructure)
- What happens when you create a wallet (step by step, from HTTP to database)
- What each project contains and why we structure it this way

No jargon — written for beginners.

---

## Folder structure

```
WalletBankingAPI/
├── START_HERE.md          ← Read this first to understand everything
├── README.md               ← This file
├── PRD.md                  ← Full product requirements (all phases)
├── database/
│   └── 001_CreateWalletTable.sql
└── src/
    ├── WalletBankingAPI.sln
    ├── WalletBankingAPI.Domain/       ← Wallet, DTOs, IWalletRepository
    ├── WalletBankingAPI.Application/   ← Commands, Queries, Handlers
    ├── WalletBankingAPI.Infrastructure/← PostgreSQL repository
    └── WalletBankingAPI.Api/           ← Controllers, Program.cs
```

---

## Other docs (when you're ready)

- **PRD.md** — Features and phases (1–5).
- **INTERVIEW_QA.md** — Interview questions and answers about the design.
- **ARCHITECTURE.md** — Deeper dive for recruiters/senior devs.
- **FROM_SCRATCH_AND_FULL_CODEBASE_GUIDE.md** — Build this solution from scratch step by step.

## Solution structure

- **WalletBankingAPI.Domain** — Entities, DTOs, `IWalletRepository` (no external deps).
- **WalletBankingAPI.Application** — Commands, Queries, MediatR handlers (refs Domain only).
- **WalletBankingAPI.Infrastructure** — `WalletRepository` (PostgreSQL, Dapper) (refs Domain only).
- **WalletBankingAPI.Api** — Controllers, middleware, DI, entry point (refs Application + Infrastructure).

See **[ARCHITECTURE.md](../ARCHITECTURE.md)** in the repo root for the full picture.

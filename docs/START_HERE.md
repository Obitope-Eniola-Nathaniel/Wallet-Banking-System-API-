# Start Here — Understand the Project (Beginner)

Read this from top to bottom. No prior knowledge of "Clean Architecture" or "CQRS" needed.

---

## What This Project Does (In One Sentence)

It’s an API that lets you **create, read, update, and delete wallets** (like bank accounts).  
You send HTTP requests (e.g. “create a wallet for Jane with 100”), and the API saves or returns data.

---

## The One Idea You Need First

We split the app into **4 layers**. Each layer has one job:

| Layer          | Job in simple words                          |
|----------------|------------------------------------------------|
| **Api**        | Receives HTTP, sends back HTTP.               |
| **Application** | Decides *what* to do (create? get one? list?). |
| **Domain**     | Defines *what a wallet is* and the *contract* for saving/loading. |
| **Infrastructure** | Actually talks to the database (PostgreSQL).   |

So: **Api** → **Application** → **Domain**, and **Infrastructure** implements what Domain says (“save/load wallets”).  
The controller (Api) never talks to the database. Only Infrastructure does.

---

## What Happens When You “Create a Wallet”?

Imagine you send: **POST /api/wallet** with body `{ "ownerName": "Jane", "initialBalance": 100 }`.

Here’s the path the request takes, step by step:

1. **Api (Controller)**  
   Receives the request. It doesn’t know how to create a wallet. It only says:  
   *“Here is a CreateWallet command (ownerName=Jane, initialBalance=100). Someone please handle it.”*

2. **MediatR**  
   A small library that acts like a post office: it takes that “command” and finds the one person who knows how to handle it — the **CreateWallet handler** in the Application layer.

3. **Application (Handler)**  
   The handler’s job:  
   - Check if the request is valid (e.g. initial balance not negative).  
   - Ask “who can save a wallet?” → It uses the **repository interface** (a contract from Domain).  
   - It says: *“Repository, please create this wallet.”*  
   It does **not** know if the repository uses PostgreSQL, Excel, or a file. It only knows the contract.

4. **Infrastructure (Repository)**  
   The **real** repository (the one we wired in the Api) uses PostgreSQL. It:  
   - Opens a connection to the database.  
   - Runs an INSERT (the SQL is in `WalletSql.cs`).  
   - Returns “success” (and maybe the new wallet id) back to the handler.

5. **Back up the chain**  
   Handler gets “success” → returns it to MediatR → Controller gets it → Controller sends an HTTP 200 and the response body to the client.

So the **flow** is:

```
You (HTTP) → Api (Controller) → MediatR → Application (Handler) → Domain contract (IWalletRepository)
                                                                         ↓
                                                          Infrastructure (WalletRepository) → PostgreSQL
```

The controller never touches the database. The handler never writes SQL. Only Infrastructure does.

---

## Why We Do It This Way

- **Easy to test**  
  We can test the “create wallet” logic by giving the handler a *fake* repository that doesn’t use a real database. So we test behavior, not the database.

- **Easy to change the database**  
  If tomorrow we want MongoDB instead of PostgreSQL, we only write a new class that implements the same `IWalletRepository` and switch it in the Api. Application and Domain don’t change.

- **Clear responsibilities**  
  Api = HTTP. Application = “what to do”. Domain = “what is a wallet and what operations exist”. Infrastructure = “how we save/load”. So when you add a new feature, you know where to put it.

---

## The 4 Projects (What Each One Contains)

### 1. Domain

- **Wallet** — The “thing”: Id, OwnerName, Balance, CreatedAt, UpdatedAt, IsDeleted. No database attributes; just a plain class.
- **ApiResponse** — A standard shape for every response (success/fail, message, data). So the API always returns the same kind of JSON.
- **DTOs** — Small classes for *input*: CreateWalletDto (OwnerName, InitialBalance), UpdateWalletDto (Id, OwnerName), DeleteWalletDto (Id).
- **IWalletRepository** — The *contract*: “Whoever implements me must be able to Create, Update, Delete, GetById, GetAll.” The contract lives in Domain so Application can use it without knowing about PostgreSQL.

Domain has **no** references to other projects or to databases. It’s the core.

---

### 2. Application

- **Commands** — “Do something.”  
  - CreateWalletCommand (it’s the create input + a marker for MediatR).  
  - CreateWalletCommandHandler: checks balance >= 0, then calls `repository.CreateAsync(...)` and returns the result.  
  Same idea for Update and Delete: Command + Handler. Handler always uses `IWalletRepository` and returns `ApiResponse`.

- **Queries** — “Get something.”  
  - GetWalletByIdQuery, GetWalletByIdQueryHandler: asks repository for one wallet; if not found, returns 404; else returns 200 with the wallet.  
  - GetAllWalletsQuery, GetAllWalletsQueryHandler: asks repository for all wallets, returns 200 with the list.

Application references **only Domain**. So it never sees “PostgreSQL” or “Npgsql”. It only sees the interface.

---

### 3. Infrastructure

- **WalletSql** — Just the SQL strings: INSERT, UPDATE (normal and soft-delete), SELECT by id, SELECT all. All use parameters (@Id, @OwnerName, etc.) so we don’t have SQL injection.

- **WalletRepository** — The class that **implements** `IWalletRepository`. It has the connection string (from config). For each method it: opens a connection, runs the right SQL with Dapper, and returns an `ApiResponse` or a `Wallet`/list. This is the **only** place in the whole solution that talks to PostgreSQL.

Infrastructure references **only Domain** (so it can use Wallet, DTOs, ApiResponse, and implement IWalletRepository).

---

### 4. Api

- **Program.cs** — When the app starts, it registers:  
  - “When someone asks for IWalletRepository, give them WalletRepository.”  
  - “Register all MediatR handlers from the Application project.”  
  So when the controller calls `mediator.Send(command)`, MediatR knows which handler to run.

- **WalletController** — One method per action (Create, Update, Delete, GetById, GetAll). Each method:  
  - Receives HTTP (and maybe body/route).  
  - Builds a Command or Query.  
  - Calls `mediator.Send(command or query)`.  
  - Returns Ok(result) or BadRequest/NotFound(result).  
  The controller does **not** use IWalletRepository. It only uses IMediator.

- **GlobalExceptionMiddleware** — If any code throws an exception that isn’t caught, this catches it and returns a single error format (500 + JSON with a message). So the client always gets a consistent error shape.

Api references Application (for MediatR and commands/queries) and Infrastructure (so it can register the real WalletRepository). So “wiring” happens only here.

---

## Summary (One Sentence Per Concept)

- **Domain** = What a wallet is and what operations exist (the contract). No database, no HTTP.
- **Application** = The “what to do” for each action (create, update, delete, get one, get all). Uses the contract only.
- **Infrastructure** = The real save/load using PostgreSQL. Implements the contract.
- **Api** = Receives HTTP, sends commands/queries via MediatR, returns HTTP. Wires repository and MediatR at startup.
- **MediatR** = Delivers each command/query to the right handler. Controller doesn’t need to know the handler.
- **IWalletRepository** = The contract (“create, update, delete, get by id, get all”). In Domain; implemented in Infrastructure.

---

## How to Run the Project

1. Install **.NET 9** and **PostgreSQL**.
2. Create a database (e.g. `WalletBanking`) and run the script in **database/001_CreateWalletTable.sql**.
3. In **src/WalletBankingAPI.Api/appsettings.Development.json**, set your PostgreSQL connection string under `ConnectionStrings:PostgreSQL`.
4. Open a terminal in **src** and run:  
   `dotnet run --project WalletBankingAPI.Api`  
5. Open the URL it shows (e.g. http://localhost:5000/swagger) and try **POST /api/wallet** with body `{ "ownerName": "Jane", "initialBalance": 100 }`.

---

## What to Read Next

- **PRD.md** — What the full project will do (all phases).  
- **README.md** — Short overview and links.  
- **FROM_SCRATCH_AND_FULL_CODEBASE_GUIDE.md** — If you want to build the same solution from scratch step by step.

Once you can say “the controller sends a command, MediatR finds the handler, the handler uses the repository interface, and the real repository talks to PostgreSQL,” you understand the whole process.

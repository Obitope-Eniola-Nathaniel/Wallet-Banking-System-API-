# How to Build This Project from Scratch & Understand Every Part

This guide does two things:

1. **Part A** — Step-by-step: how to create the entire solution yourself (projects, references, files in order).
2. **Part B** — Full codebase walkthrough: what every project, folder, and file does, and how a request flows from HTTP to the database and back.

Use Part A when you want to rebuild from zero. Use Part B when you want to understand every piece of the existing code.

---

# Part A: Create the Project from Scratch

## What You Need

- **.NET 9 SDK** (or .NET 8; adjust `<TargetFramework>` if needed).
- **PostgreSQL** installed (or a cloud DB). You only need a database and the script in `database/001_CreateWalletTable.sql`.
- **IDE:** Visual Studio 2022 or VS Code with C# extension (or any editor; we use `dotnet` CLI below).

---

## Step 1: Create the Folder and Solution

1. Create a folder, e.g. `WalletBankingAPI`, and inside it create `src`.
2. Open a terminal in `src` and run:

```bash
dotnet new sln -n WalletBankingAPI
```

This creates `WalletBankingAPI.sln`. The solution is just a container for multiple projects.

---

## Step 2: Create the Four Projects

We create four projects in **dependency order**: Domain first (no dependencies), then Application (depends on Domain), then Infrastructure (depends on Domain), then Api (depends on Application and Infrastructure).

**2.1 Domain (class library, no web, no extra packages)**

```bash
dotnet new classlib -n WalletBankingAPI.Domain -f net9.0
dotnet sln add WalletBankingAPI.Domain/WalletBankingAPI.Domain.csproj
```

- Open `WalletBankingAPI.Domain.csproj`. Remove any `PackageReference` so the project has **no** NuGet packages. Domain must stay pure (no EF, no MediatR, no database drivers).
- Optional: add `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` in a `<PropertyGroup>`.

**2.2 Application (class library, MediatR only)**

```bash
dotnet new classlib -n WalletBankingAPI.Application -f net9.0
dotnet sln add WalletBankingAPI.Application/WalletBankingAPI.Application.csproj
```

- Open `WalletBankingAPI.Application.csproj`. Add:
  - Package: `MediatR` (e.g. 12.4.1).
  - Project reference: `WalletBankingAPI.Domain` only. **Do not** reference Infrastructure.

**2.3 Infrastructure (class library, Dapper + Npgsql)**

```bash
dotnet new classlib -n WalletBankingAPI.Infrastructure -f net9.0
dotnet sln add WalletBankingAPI.Infrastructure/WalletBankingAPI.Infrastructure.csproj
```

- Open `WalletBankingAPI.Infrastructure.csproj`. Add:
  - Packages: `Dapper`, `Npgsql`, `Microsoft.Extensions.Configuration.Abstractions`.
  - Project reference: `WalletBankingAPI.Domain` only.

**2.4 Api (web API)**

```bash
dotnet new webapi -n WalletBankingAPI.Api -f net9.0
dotnet sln add WalletBankingAPI.Api/WalletBankingAPI.Api.csproj
```

- Open `WalletBankingAPI.Api.csproj`. Add:
  - Packages: `MediatR`, `Swashbuckle.AspNetCore` (Swagger).
  - Project references: `WalletBankingAPI.Application` and `WalletBankingAPI.Infrastructure` (both). Api is the only project that references Infrastructure; that’s the “composition root.”

---

## Step 3: Create Domain Layer Files

Domain has **no** reference to any other project. It only defines entities, DTOs, common types, and the repository **interface**.

**3.1 Entity: Wallet**

- In Domain project, create folder `Entities` and add `Wallet.cs`:

```csharp
namespace WalletBankingAPI.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

- **Why:** This is the core business concept. No `[Key]` or database attributes — Domain stays free of persistence details. `decimal` for money avoids rounding errors.

**3.2 Common: ApiResponse**

- Create folder `Common`, add `ApiResponse.cs`:

```csharp
namespace WalletBankingAPI.Domain.Common;

public class ApiResponse
{
    public int StatusCode { get; set; } = 500;
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public string? ResponseCode { get; set; } = "99";
    public object? Data { get; set; }
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}
```

- **Why:** Every API response (success or failure) has the same shape so clients and middleware can handle them consistently.

**3.3 DTOs (data transfer objects)**

- Create folder `Dto` and add:

- `CreateWalletDto.cs`: properties `OwnerName`, `InitialBalance`.
- `UpdateWalletDto.cs`: properties `Id`, `OwnerName`.
- `DeleteWalletDto.cs`: property `Id`.
- `GetWalletByIdDto.cs`: property `Id` (optional; we use `Guid` directly in the query in this project).

DTOs are the **input** for use cases. They keep the API/application contract separate from the entity.

**3.4 Repository interface**

- Create folder `Repositories`, add `IWalletRepository.cs`:

```csharp
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Dto;
using WalletBankingAPI.Domain.Entities;

namespace WalletBankingAPI.Domain.Repositories;

public interface IWalletRepository
{
    Task<ApiResponse> CreateAsync(CreateWalletDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> UpdateAsync(UpdateWalletDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteWalletDto dto, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wallet>> GetAllAsync(CancellationToken cancellationToken = default);
}
```

- **Why in Domain?** So that **Application** can depend on this interface without depending on Infrastructure. Infrastructure will implement it. This is the Dependency Inversion Principle: depend on abstractions, not concrete classes.

---

## Step 4: Create Application Layer Files

Application contains **use cases**: one command or query per operation, each with one handler. Handlers use only `IWalletRepository` (and Domain types); they do **not** reference Infrastructure.

**4.1 Commands (write operations)**

For each command you need:
- A **command class** (holds input data, implements `IRequest<ApiResponse>`).
- A **handler class** (implements `IRequestHandler<TCommand, ApiResponse>`).

**Create Wallet**

- Create folder `Commands/CreateWallet`:
  - `CreateWalletCommand.cs`: inherit from `CreateWalletDto`, add `IRequest<ApiResponse>`. So the command is literally the DTO + MediatR marker.
  - `CreateWalletCommandHandler.cs`: constructor takes `IWalletRepository`. In `Handle`, validate (e.g. `InitialBalance >= 0`), then call `await walletRepository.CreateAsync(request, cancellationToken)` and return the result.

**Update Wallet**

- Folder `Commands/UpdateWallet`: `UpdateWalletCommand` (from `UpdateWalletDto`, `IRequest<ApiResponse>`), `UpdateWalletCommandHandler` (calls `repository.UpdateAsync`).

**Delete Wallet**

- Folder `Commands/DeleteWallet`: `DeleteWalletCommand`, `DeleteWalletCommandHandler` (calls `repository.DeleteAsync`).

**4.2 Queries (read operations)**

**Get by Id**

- Folder `Queries/GetWalletById`:
  - `GetWalletByIdQuery.cs`: property `Id`, implements `IRequest<ApiResponse>`.
  - `GetWalletByIdQueryHandler.cs`: call `repository.GetByIdAsync(request.Id)`. If null, return 404 `ApiResponse`; else return 200 `ApiResponse` with `Data = wallet`.

**Get All**

- Folder `Queries/GetAllWallets`:
  - `GetAllWalletsQuery.cs`: no properties, implements `IRequest<ApiResponse>`.
  - `GetAllWalletsQueryHandler.cs`: call `repository.GetAllAsync()`, return `ApiResponse` with `Data = list`.

**Why MediatR?** The controller only does `mediator.Send(command)` or `mediator.Send(query)`. MediatR finds the right handler by the type of the request. So the controller never talks to the repository or domain directly — that’s Clean Architecture.

---

## Step 5: Create Infrastructure Layer Files

Infrastructure **implements** `IWalletRepository` and talks to PostgreSQL.

**5.1 SQL**

- Create folder `Persistence`, add `WalletSql.cs`: static class with `const string` for Create (INSERT), Update, SoftDelete, GetById (SELECT one), GetAll (SELECT list). Use parameterized queries (`@Id`, `@OwnerName`, etc.) to avoid SQL injection. Use quoted identifiers if your column names are case-sensitive (e.g. `"Wallet"`, `"OwnerName"`).

**5.2 WalletRepository**

- Create folder `Repositories`, add `WalletRepository.cs`:
  - Constructor: inject `IConfiguration`, read connection string (e.g. `GetConnectionString("PostgreSQL")`).
  - `CreateAsync`: create a `Wallet` instance (new `Guid`, set `CreatedAt`/`UpdatedAt`, `IsDeleted = false`), open `NpgsqlConnection`, run `conn.ExecuteAsync(WalletSql.Create, wallet)`, return `ApiResponse` success with optional `Data`.
  - `UpdateAsync`: run UPDATE SQL with `dto.Id`, `dto.OwnerName`, `UpdatedAt`. If `ExecuteAsync` returns 0 rows updated, return 404 `ApiResponse`; else 200.
  - `DeleteAsync`: run soft-delete UPDATE. Same idea: 0 rows → 404, else 200.
  - `GetByIdAsync`: `conn.QuerySingleOrDefaultAsync<Wallet>(WalletSql.GetById, new { Id = id })`. Return the wallet or null.
  - `GetAllAsync`: `conn.QueryAsync<Wallet>(WalletSql.GetAll)`, return `.ToList()`.

Dapper maps column names to `Wallet` properties automatically when names match (or use quoted names in SQL to match C# PascalCase).

---

## Step 6: Create Api Layer Files

**6.1 Program.cs**

- Add services:
  - `AddScoped<IWalletRepository, WalletRepository>()` — so every request gets one repository instance.
  - `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateWalletCommand).Assembly))` — register all handlers from the Application assembly.
  - `AddControllers()`, `AddSwaggerGen(...)`.
- Build the app, then:
  - `app.UseMiddleware<GlobalExceptionMiddleware>();` (create this next).
  - Swagger in Development, `UseHttpsRedirection`, `UseAuthorization`, `MapControllers`, `app.Run()`.

**6.2 GlobalExceptionMiddleware**

- Create folder `Extensions`, add `GlobalExceptionMiddleware.cs`:
  - Constructor: `RequestDelegate _next`, `ILogger<...>`.
  - `InvokeAsync`: try `await _next(context)`; in catch, log the exception and call a helper that sets response status 500, content-type JSON, and writes an `ApiResponse` (IsSuccessful = false, Message generic, etc.) as JSON. This way any unhandled exception becomes a consistent error response.

**6.3 WalletController**

- Create folder `Controllers`, add `WalletController.cs`:
  - Constructor: inject `IMediator mediator`.
  - `[Route("api/[controller]")]` so base route is `api/Wallet`.
  - **Create:** `[HttpPost]` — action takes `[FromBody] CreateWalletCommand command`, then `await mediator.Send(command, cancellationToken)`. Return `Ok(result)` if successful, else `BadRequest(result)`.
  - **Update:** `[HttpPut("{id:guid}")]` — take `id` from route and body with `OwnerName`. Build `UpdateWalletCommand`, send, return `Ok` or `NotFound`.
  - **Delete:** `[HttpDelete("{id:guid}")]` — build `DeleteWalletCommand` with id, send, return `Ok` or `NotFound`.
  - **GetById:** `[HttpGet("{id:guid}")]` — build `GetWalletByIdQuery`, send, return `Ok` or `NotFound`.
  - **GetAll:** `[HttpGet]` — send `new GetAllWalletsQuery()`, return `Ok(result)`.

Controller has **no** reference to `IWalletRepository` or any Infrastructure type. It only uses `IMediator` and command/query types.

**6.4 Configuration**

- In `appsettings.json` and `appsettings.Development.json`, add under `ConnectionStrings` a key `"PostgreSQL"` with your connection string (Host, Port, Database, Username, Password).

---

## Step 7: Database

- Create the database in PostgreSQL (e.g. `WalletBanking`).
- Run the script that creates the `Wallet` table (same columns as the entity: Id UUID, OwnerName, Balance NUMERIC, CreatedAt, UpdatedAt, IsDeleted). You can keep this script in a `database` folder at the repo root (e.g. `database/001_CreateWalletTable.sql`).

---

## Step 8: Build and Run

- From `src`: `dotnet build`. Fix any missing namespaces or references.
- Run: `dotnet run --project WalletBankingAPI.Api`. Open the URL shown (e.g. `/swagger`) and test POST/GET/PUT/DELETE.

You have now built the same structure from scratch. Below is how every part of this codebase works together.

---

# Part B: Understanding Every Part of the Codebase

## The Big Picture: What Happens When Someone Creates a Wallet?

1. Client sends **HTTP POST** to `/api/wallet` with JSON body `{ "ownerName": "Jane", "initialBalance": 100 }`.
2. **ASP.NET Core** routes the request to `WalletController.Create`.
3. The framework **binds** the JSON body to a `CreateWalletCommand` (because the action parameter type is `CreateWalletCommand` and it has `OwnerName` and `InitialBalance`).
4. The controller calls **`mediator.Send(command)`**. The controller does nothing else with the data.
5. **MediatR** looks up the handler registered for `CreateWalletCommand`. That is `CreateWalletCommandHandler`.
6. **CreateWalletCommandHandler** runs:
   - Validates (e.g. initial balance >= 0).
   - Calls **`walletRepository.CreateAsync(command)`**. The handler does not know if the repository is PostgreSQL, MongoDB, or a fake — it only knows the interface.
7. **WalletRepository** (in Infrastructure) runs:
   - Builds a `Wallet` object (new Id, OwnerName, Balance, timestamps, IsDeleted = false).
   - Opens a connection to PostgreSQL using the connection string from config.
   - Executes the INSERT SQL (from `WalletSql.Create`) with that wallet.
   - Returns an **ApiResponse** (success, message, optional Data).
8. That response goes back: Repository → Handler → MediatR → Controller. The controller returns **Ok(result)** so the client gets 200 and the same `ApiResponse` as JSON.
9. If any step throws an exception that is not caught, **GlobalExceptionMiddleware** catches it, logs it, and returns a 500 response with an `ApiResponse`-shaped JSON body so the client always gets a consistent error format.

So: **HTTP → Controller → MediatR → Handler → Repository → Database**, and the response flows back the same path. The controller and the handler never open a database connection; only the repository does.

---

## Project-by-Project and File-by-File

### WalletBankingAPI.Domain

**Purpose:** Define the core business concepts and the contract for data access. No dependencies on other projects or on frameworks (no EF, no MediatR, no Npgsql).

| File | What it is | Why it exists |
|------|------------|----------------|
| **Entities/Wallet.cs** | The wallet entity: Id, OwnerName, Balance, CreatedAt, UpdatedAt, IsDeleted. | This is the “thing” the app manages. Kept free of database attributes so Domain stays persistence-ignorant. |
| **Common/ApiResponse.cs** | A wrapper with IsSuccessful, Message, ResponseCode, StatusCode, Data, ResponseTime. | Every API response (from handlers and middleware) uses this shape so clients and error handling are consistent. |
| **Dto/CreateWalletDto.cs** | OwnerName, InitialBalance. | Input for “create wallet” use case. Separates API/use-case contract from the entity. |
| **Dto/UpdateWalletDto.cs** | Id, OwnerName. | Input for “update wallet.” |
| **Dto/DeleteWalletDto.cs** | Id. | Input for “delete wallet.” |
| **Dto/GetWalletByIdDto.cs** | Id. | Can be used for “get by id”; in this project the query uses a simple Guid. |
| **Repositories/IWalletRepository.cs** | Interface with CreateAsync, UpdateAsync, DeleteAsync, GetByIdAsync, GetAllAsync. | Application depends on this **abstraction**. Infrastructure implements it. So Application never references Infrastructure, and you can test or swap implementations easily. |

**Dependency rule:** Domain references **nothing**. No project reference, no NuGet (unless you add something like a tiny validation library that has no I/O). So Domain is the “innermost” layer.

---

### WalletBankingAPI.Application

**Purpose:** Implement use cases (one command or query per operation). Each use case is a request (command/query) + a handler. Handlers use only `IWalletRepository` and Domain types.

| Folder / File | What it is | Why it exists |
|---------------|------------|----------------|
| **Commands/CreateWallet/CreateWalletCommand.cs** | Class that has OwnerName and InitialBalance (from CreateWalletDto) and implements `IRequest<ApiResponse>`. | MediatR uses the **type** of the request to find the handler. So “CreateWalletCommand” is the “message” for “create wallet.” |
| **Commands/CreateWallet/CreateWalletCommandHandler.cs** | Implements `IRequestHandler<CreateWalletCommand, ApiResponse>`. Constructor gets `IWalletRepository`. In Handle: if InitialBalance < 0 return error ApiResponse; else return result of `walletRepository.CreateAsync(request)`. | Single place for “create wallet” logic. Validation (balance >= 0) can live here or in Domain; repository does the actual persistence. |
| **Commands/UpdateWallet/** | UpdateWalletCommand (Id, OwnerName), UpdateWalletCommandHandler (calls repository.UpdateAsync). | Same pattern: command carries input, handler orchestrates and calls repository. |
| **Commands/DeleteWallet/** | DeleteWalletCommand, DeleteWalletCommandHandler. | Same pattern for soft delete. |
| **Queries/GetWalletById/GetWalletByIdQuery.cs** | Has property Id, implements IRequest<ApiResponse>. | Read request: “give me the wallet with this id.” |
| **Queries/GetWalletById/GetWalletByIdQueryHandler.cs** | Calls repository.GetByIdAsync(request.Id). If null → 404 ApiResponse; else 200 ApiResponse with Data = wallet. | Converts “wallet or null” into a consistent ApiResponse (success + data or failure + message). |
| **Queries/GetAllWallets/** | GetAllWalletsQuery (no data), GetAllWalletsQueryHandler (calls GetAllAsync, returns ApiResponse with Data = list). | List use case. |

**Dependency rule:** Application references **only Domain**. So it can use `IWalletRepository`, `Wallet`, `ApiResponse`, DTOs. It does **not** reference Infrastructure or Api. That’s why the handler never sees “PostgreSQL” or “Npgsql” — it only sees the interface.

---

### WalletBankingAPI.Infrastructure

**Purpose:** Implement `IWalletRepository` and any other “outbound” concerns (database, file system, etc.). Only this layer talks to PostgreSQL.

| File | What it is | Why it exists |
|------|------------|----------------|
| **Persistence/WalletSql.cs** | Static class with string constants for SQL: INSERT (Create), UPDATE (Update and SoftDelete), SELECT (GetById, GetAll). Uses parameters like @Id, @OwnerName. | Central place for SQL; easy to change or add indexes later. Parameterized queries prevent SQL injection. |
| **Repositories/WalletRepository.cs** | Implements `IWalletRepository`. Constructor takes `IConfiguration`, reads connection string. Each method opens `NpgsqlConnection`, uses Dapper to Execute or Query, maps results to `Wallet`, returns `ApiResponse` or `Wallet?` / list. | This is the **only** class that knows about PostgreSQL and Dapper. CreateAsync builds a Wallet, runs INSERT, returns success. Update/Delete run UPDATE and check affected rows for 404. GetById uses QuerySingleOrDefaultAsync; GetAll uses QueryAsync. |

**Dependency rule:** Infrastructure references **only Domain**. So it can use `Wallet`, DTOs, `ApiResponse`, and `IWalletRepository`. It does **not** reference Application or Api. So “database” is a detail that stays in this layer.

---

### WalletBankingAPI.Api

**Purpose:** HTTP entry point: routing, controllers, middleware, and **wiring** (dependency injection). This is the “composition root”: the only place that knows about the concrete `WalletRepository` and registers it for `IWalletRepository`.

| File | What it is | Why it exists |
|------|------------|----------------|
| **Program.cs** | Creates the web app builder, registers: IWalletRepository → WalletRepository (Scoped); MediatR with assembly from Application; Controllers; Swagger. Builds app, adds GlobalExceptionMiddleware, then Swagger (in Dev), HttpsRedirection, Authorization, MapControllers, Run. | **DI registration:** “When anyone asks for IWalletRepository, give them WalletRepository.” MediatR registration: “Find all IRequestHandler in Application assembly and register them.” So when the controller calls mediator.Send(command), MediatR finds CreateWalletCommandHandler and runs it. |
| **Extensions/GlobalExceptionMiddleware.cs** | Middleware: in InvokeAsync, calls await _next(context) inside try; on exception, logs and sends back a 500 response with ApiResponse (IsSuccessful false, generic message) as JSON. | So unhandled exceptions (e.g. DB down, bug) don’t leak stack traces to the client; they get a consistent error body. |
| **Controllers/WalletController.cs** | Has IMediator. Create: receives CreateWalletCommand from body, Send(command), return Ok or BadRequest. Update: id from route + body OwnerName, build UpdateWalletCommand, Send, Ok or NotFound. Delete: id from route, DeleteWalletCommand, Send, Ok or NotFound. GetById: id from route, GetWalletByIdQuery, Send, Ok or NotFound. GetAll: Send(new GetAllWalletsQuery()), Ok. | Controllers are **thin**: they only translate HTTP to a command/query and send it to MediatR, then map the result to HTTP status. No repository, no SQL, no business logic — that’s what recruiters and seniors look for. |
| **appsettings.json / appsettings.Development.json** | JSON with Logging, AllowedHosts, ConnectionStrings:PostgreSQL. | Configuration. Infrastructure’s WalletRepository uses IConfiguration to read the connection string at runtime. |

**Dependency rule:** Api references **Application** (for MediatR and command/query types) and **Infrastructure** (to register WalletRepository). So the “composition root” is in Api: it’s the only place that ties the interface to the concrete implementation.

---

## How the Layers Connect (Dependency Rule)

- **Domain** ← referenced by Application and Infrastructure. Domain references nothing.
- **Application** ← referenced by Api. Application references only Domain (so it uses IWalletRepository, Wallet, ApiResponse, DTOs).
- **Infrastructure** ← referenced by Api only. Infrastructure references only Domain (so it can implement IWalletRepository and use Wallet, DTOs, ApiResponse).
- **Api** references Application and Infrastructure. It never references Domain directly in code (it gets Domain types indirectly through Application and Infrastructure).

So:

- **Controller** → uses MediatR and command/query types (from Application).
- **Handler** → uses IWalletRepository (from Domain) and DTOs/entities (from Domain).
- **WalletRepository** → implements IWalletRepository (from Domain), uses SQL and Npgsql (in Infrastructure).

No circular dependencies. Business logic (Application + Domain) does not depend on databases or HTTP; those are in Infrastructure and Api.

---

## Key Concepts in One Sentence Each

- **Domain:** The heart: entities and the repository interface. No dependencies.
- **Application:** Use cases: one command/query + one handler per operation; handlers call the repository interface.
- **Infrastructure:** Implements the repository; talks to PostgreSQL (or any data store).
- **Api:** Entry point: controllers send commands/queries via MediatR; DI wires repository implementation to interface.
- **MediatR:** Sends a “request” (command or query); the right “handler” runs. Controller doesn’t need to know which handler.
- **IWalletRepository:** Abstraction so Application doesn’t depend on Infrastructure. Defined in Domain, implemented in Infrastructure.
- **ApiResponse:** Same JSON shape for every response (success or failure) so clients and middleware are consistent.
- **GlobalExceptionMiddleware:** Catches unhandled exceptions and returns a single error format (e.g. 500 + ApiResponse).

---

## What to Do Next

- **Run the API:** Set the PostgreSQL connection string, create the DB and run the SQL script, then `dotnet run --project WalletBankingAPI.Api`. Test with Swagger.
- **Trace a request:** Put a breakpoint in `WalletController.Create`, then in `CreateWalletCommandHandler.Handle`, then in `WalletRepository.CreateAsync`, and step through to see the flow.
- **Phase 2:** Add Deposit/Withdraw commands and handlers, and enforce “balance >= 0” and “amount > 0” in the handler or in the domain.

Once you can explain “HTTP → Controller → MediatR → Handler → IWalletRepository → WalletRepository → Database” and why each layer exists, you understand the whole codebase.

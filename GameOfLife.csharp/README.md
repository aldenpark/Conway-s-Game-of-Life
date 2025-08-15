# Conway's Game of Life — C# API & CLI

A .NET 8 implementation of Conway’s Game of Life with:

- **ASP.NET Core Web API** (Fast endpoints + Swagger)
- **SQLite persistence** (EF Core)
- **Cycle detection** (final state returns fixed/cycle with period)
- **Guardrails** (max dims/cell count via env)
- **Structured logging** (middleware writes JSON lines)
- **CLI** (animated or snapshot runs)

---

## Project layout
```
GameOfLife.csharp/
├─ src/
│  └─ Life.Api/
│     ├─ Life.Api.csproj
│     ├─ Program.cs
│     ├─ Controllers/
│     │  └─ BoardsController.cs
│     ├─ Data/
│     │  ├─ Entities.cs
│     │  ├─ LifeDbContext.cs
│     │  └─ BoardRepository.cs
│     ├─ Domain/
│     │  ├─ LifeConfig.cs
│     │  └─ GameOfLife.cs
│     ├─ DTOs/
│     │  ├─ Requests.cs
│     │  └─ Responses.cs
│     └─ Infrastructure/
│        ├─ Guardrails.cs
│        ├─ ServiceCollectionExtensions.cs
│        ├─ RequestLoggingMiddleware.cs
│        └─ ApplicationBuilderExtensions.cs
├─ tests/
│  └─ Life.Api.Tests/
│     ├─ Life.Api.Tests.csproj
│     ├─ GameOfLifeTests.cs
│     ├─ ApiTests.cs
│     └─ GuardrailTests.cs
├─ src/
   └─ Life.Cli/
      ├─ Life.Cli.csproj
      └─ Program.cs              CLI runner
```

---

## Prerequisites

- .NET SDK 8.0
- SQLite (no server required; file-based)
- (Linux/macOS) bash for the quick commands below

---

## One-time setup
### from your repo root
mkdir -p GameOfLife.csharp/src GameOfLife.csharp/tests
cd GameOfLife.csharp

### API project
dotnet new web -n src/Life.Api -f net8.0

### Solution
dotnet new sln -n Life
dotnet sln add src/Life.Api/Life.Api.csproj

### Test project
dotnet new xunit -n tests/Life.Api.Tests -f net8.0
dotnet sln add tests/Life.Api.Tests/Life.Api.Tests.csproj
dotnet add tests/Life.Api.Tests/Life.Api.Tests.csproj reference src/Life.Api/Life.Api.csproj

### Packages
dotnet add src/Life.Api/Life.Api.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/Life.Api/Life.Api.csproj package Swashbuckle.AspNetCore
### (only if you use [SwaggerOperation]/annotations)
### dotnet add src/Life.Api/Life.Api.csproj package Swashbuckle.AspNetCore.Annotations

dotnet add tests/Life.Api.Tests/Life.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Life.Api.Tests/Life.Api.Tests.csproj package FluentAssertions
dotnet add tests/Life.Api.Tests/Life.Api.Tests.csproj package Microsoft.Data.Sqlite
dotnet add tests/Life.Api.Tests/Life.Api.Tests.csproj package Microsoft.EntityFrameworkCore.Sqlite

### Optional CLI project (if you want a .NET console visualizer)
dotnet new console -n src/Life.Cli -f net8.0
dotnet sln add src/Life.Cli/Life.Cli.csproj
dotnet add src/Life.Cli/Life.Cli.csproj reference src/Life.Api/Life.Api.csproj

---

### Build & Test

cd GameOfLife.csharp

dotnet restore
dotnet build
dotnet test

---

## Run the API

### (optional) set DB and guardrail limits
export LIFE_DB_PATH=/tmp/life.db
export LIFE_MAX_DIM=1000
export LIFE_MAX_CELLS=1000000

## start API
dotnet run --project src/Life.Api/Life.Api.csproj

Expected:
info: Microsoft.Hosting.Lifetime[...] Now listening on: http://0.0.0.0:5000

---

## Swagger / Health

- Swagger UI: http://localhost:5000/swagger
  - If you set RoutePrefix = "" in your SwaggerUI options, the UI is at `/` instead.
- Health:     http://localhost:5000/health  (or /healthz based on your controller)

---

## API Quickstart (curl)

### 1) Create (random)
curl -s -X POST http://localhost:5000/boards \
  -H 'Content-Type: application/json' \
  -d '{"height":25,"width":40,"density":0.25,"wrap":true}'

### 2) Get current
curl -s "http://localhost:5000/boards/<BOARD_ID>"

### 3) Next generation
curl -s "http://localhost:5000/boards/<BOARD_ID>/next"

### 4) Advance N generations
curl -s "http://localhost:5000/boards/<BOARD_ID>/advance?n=100"

### 5) Final (fixed or cycle)
curl -s "http://localhost:5000/boards/<BOARD_ID>/final?max_iters=200000"

```
Response meta examples:
{ "meta": { "status": "fixed" } }
{ "meta": { "status": "cycle", "period": 2 } }

Payload (custom grid):
{
  "grid": [
    [0,1,0],
    [1,1,1],
    [0,1,0]
  ],
  "wrap": true
}
```
---

## Logging

The middleware writes one JSON line per request to stdout. To save:

mkdir -p logs
dotnet run --project src/Life.Api/Life.Api.csproj | tee logs/life.jsonl

Example line:
{ "level":"INFO","message":"request","logger":"life.api","method":"GET","path":"/boards/ABC/next","status":200,"duration_ms":3,"client":"127.0.0.1" }

---

## *Optionl Configuration (env)
```
LIFE_DB_PATH    : SQLite file path (default: life.db)
LIFE_MAX_DIM    : Max allowed height/width per side (default: 1000)
LIFE_MAX_CELLS  : Max allowed height*width (default: 1000000)
ASPNETCORE_URLS : Kestrel listen URLs (e.g., http://0.0.0.0:5000)
```
---

## CLI (optional)

### Animated random run
dotnet run --project src/Life.Cli/Life.Cli.csproj -- --animate --width 80 --height 30 --density 0.25 --fps 15

### One-shot snapshot after N steps
dotnet run --project src/Life.Cli/Life.Cli.csproj -- --width 60 --height 25 --steps 200

### Reproducible (seed)
dotnet run --project src/Life.Cli/Life.Cli.csproj -- --animate --seed 42

### No wrapping and custom chars (dead, alive)
dotnet run --project src/Life.Cli/Life.Cli.csproj -- --animate --nowrap --chars=" ·█"

### Help:
dotnet run --help to see additional configuration options

---

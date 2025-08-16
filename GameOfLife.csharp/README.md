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
.github/
├─ workflows/
│  ├─ donnet.yml
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
│  └─ Life.Cli/
│     ├─ Life.Cli.csproj
│     └─ Program.cs              CLI runner
├─ Life.sln
├─ README.md
└─ smoke.sh
```

---

## Prerequisites

- .NET SDK 8.0
- SQLite (no server required; file-based)
- (Linux/macOS) bash for the quick commands below

---

### from repo root
```bash
cd GameOfLife.csharp
```

### make sure .NET 8 SDK is available
```bash
dotnet --info
```

### restore / build / test / run the API
```bash
dotnet restore Life.sln
dotnet build Life.sln -c Release
dotnet test tests/Life.Api.Tests/Life.Api.Tests.csproj -c Release
```

Expected:
info: Microsoft.Hosting.Lifetime[...] Now listening on: http://0.0.0.0:5000

---

## Home /Swagger / Health

- Home:       http://localhost:5000/
- Swagger UI: http://localhost:5000/docs
- Health:     http://localhost:5000/health

---

## API Quickstart (curl)

### 1) Create (random)
```bash
curl -s -X POST http://localhost:5000/boards \
  -H 'Content-Type: application/json' \
  -d '{"height":25,"width":40,"density":0.25,"wrap":true}'
```

### 2) Get current
```bash
curl -s "http://localhost:5000/boards/<BOARD_ID>"
```

### 3) Next generation
```bash
curl -s "http://localhost:5000/boards/<BOARD_ID>/next"
```

### 4) Advance N generations
```bash
curl -s "http://localhost:5000/boards/<BOARD_ID>/advance?n=100"
```

### 5) Final (fixed or cycle)
```bash
curl -s "http://localhost:5000/boards/<BOARD_ID>/final?max_iters=200000"
```

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

## Smoke test (smoke.sh)

A tiny bash script to exercise all endpoints end-to-end.

```bash
chmod +x smoke.sh
./smoke.sh
```

---


## Logging

The middleware writes one JSON line per request to stdout. To save:

```bash
mkdir -p logs
dotnet run --project src/Life.Api/Life.Api.csproj | tee logs/life.jsonl

Example line:
{ "level":"INFO","message":"request","logger":"life.api","method":"GET","path":"/boards/ABC/next","status":200,"duration_ms":3,"client":"127.0.0.1" }
```

---

## (optional) Set DB and guardrail limits
```bash
LIFE_DB_PATH    : SQLite file path (default: life.db)
LIFE_MAX_DIM    : Max allowed height/width per side (default: 1000)
LIFE_MAX_CELLS  : Max allowed height*width (default: 1000000)
ASPNETCORE_URLS : Kestrel listen URLs (e.g., http://0.0.0.0:5000)
```
---

## CLI (optional)

### Animated random run
```bash
dotnet run --project src/Life.Cli/Life.Cli.csproj -- --animate --width 80 --height 30 --density 0.25 --fps 15
```

### One-shot snapshot after N steps
```bash
dotnet run --project src/Life.Cli/Life.Cli.csproj -- --width 60 --height 25 --steps 200
```

### Reproducible (seed)
```bash
dotnet run --project src/Life.Cli/Life.Cli.csproj -- --animate --seed 42
```

### No wrapping and custom chars (dead, alive)
```bash
dotnet run --project src/Life.Cli/Life.Cli.csproj -- --animate --nowrap --chars=" ·█"
```

### Help:
```bash
dotnet run --help to see additional configuration options
```

---

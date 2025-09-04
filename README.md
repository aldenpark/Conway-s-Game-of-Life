# Conway-s-Game-of-Life

## Multi-language implementations of Conway’s Game of Life with a shared API contract, persistence, and tests.

Folders
-------------------------------------------------------------------------------
- [GameOfLife.csharp/](GameOfLife.csharp/)      → .NET 8 Web API (SQLite + Swagger + tests)
- [GameOfLife.nodejs/](GameOfLife.nodejs/)      → Node.js/Express API + SQLite + CLI
- GameOfLife.python/     → (planned) FastAPI service + CLI
- GameOfLife.golang/     → (planned) Go HTTP service + CLI

-------------------------------------------------------------------------------
C# (implemented) — highlights
-------------------------------------------------------------------------------
- ASP.NET Core Web API with Swagger UI
- EF Core + SQLite persistence
- Cycle detection (final state returns meta.status = fixed|cycle and meta.period)
- Guardrails (LIFE_MAX_DIM, LIFE_MAX_CELLS)
- Structured JSON logging middleware
- Tests: rules, cycle/fixed, endpoints, guardrails

```
# Quick start
  cd GameOfLife.csharp
  dotnet restore
  dotnet build
  dotnet test

# Run API (default: http://localhost:5000):
  export ASPNETCORE_URLS=http://0.0.0.0:5000
  export LIFE_DB_PATH=/tmp/life.db
  export LIFE_MAX_DIM=1000
  export LIFE_MAX_CELLS=1000000
  dotnet run --project src/Life.Api/Life.Api.csproj
```

Swagger:  http://localhost:5000/swagger
Health:   http://localhost:5000/health

See the C# README for full details:
./GameOfLife.csharp/README.md

-------------------------------------------------------------------------------
Node.js (implemented) — highlights
-------------------------------------------------------------------------------
- Express HTTP API with SQLite persistence and concise pino JSON logs
- Shared domain logic (wrap/nowrap, RNG seeding, cycle/fixed final state)
- CLI (`life-cli`) for terminal rendering (animate or step N)
- Guardrails via env (`MAX_DIM`, `MAX_CELLS`), same API contract as C#
- New: comprehensive in-code comments documenting design and behavior in:
  - `src/models/life.js` (rules, neighbors, RNG seed, cycle detection)
  - `src/db/repository.js` (schema, WAL, gzip pack/unpack, upserts)
  - `src/routes/boards.js` (endpoint semantics, validation, guardrails)
  - `src/logger.js` (log shape, serializers), `src/server.js` (bootstrap)
  - `bin/life-cli.js` (usage/animation rendering)

```
# Quick start
  cd GameOfLife.nodejs
  npm install
  npm run dev             # http://localhost:8000
# or
  npm start               # production mode

# CLI examples
  npx life-cli --height 20 --width 50 --density 0.3 --steps 10
  npx life-cli --animate --fps 12 --seed 42
```

Health:   http://localhost:8000/health
README:   ./GameOfLife.nodejs/README.md

-------------------------------------------------------------------------------
Python (upcoming) — plan
-------------------------------------------------------------------------------
Folder: ./GameOfLife.python/

Planned stack
- Python 3.10+
- FastAPI + Uvicorn
- SQLite (sqlite3) storing compressed NumPy npy payloads
- CLI renderer (terminal animation / snapshot)
- pytest + httpx tests mirroring the C# test coverage
- Structured logging to stdout

Planned endpoints (same contract as C#)
- POST   /boards                  (grid OR height/width/density/seed, wrap)
- GET    /boards/{id}             (current state)
- GET    /boards/{id}/next        (advance 1)
- GET    /boards/{id}/advance?n=K (advance K)
- GET    /boards/{id}/final       (stop at fixed/cycle; returns meta.status and meta.period)

Guardrails (env)
- LIFE_DB_PATH, LIFE_MAX_DIM, LIFE_MAX_CELLS

Notes
- Cycle detection uses SHA-256 of compressed state.
- CLI supports --animate, --steps, --chars, --nowrap, --seed.

-------------------------------------------------------------------------------
Go (upcoming) — plan
-------------------------------------------------------------------------------
Folder: ./GameOfLife.golang/

Planned stack
- Go 1.22+
- HTTP router: chi (or Gin)
- SQLite (modernc.org/sqlite or mattn/go-sqlite3)
- JSON logging (structured)
- Tests with `go test`, table-driven for rules and handlers

Planned endpoints (same contract as C#)
- POST   /boards
- GET    /boards/{id}
- GET    /boards/{id}/next
- GET    /boards/{id}/advance?n=K
- GET    /boards/{id}/final

Guardrails (env)
- LIFE_DB_PATH, LIFE_MAX_DIM, LIFE_MAX_CELLS

Notes
- Domain logic in a pure package; handlers thin; repository interface over SQLite.

-------------------------------------------------------------------------------
Common API shapes
-------------------------------------------------------------------------------
Create (random):
  { "height": 25, "width": 40, "density": 0.25, "wrap": true, "seed": null }

Create (grid):
  { "grid": [[0,1,0],[1,1,1],[0,1,0]], "wrap": true }

Responses:
BoardResponse:
  { "board_id": "abc123" }

GridResponse:
  {
    "board_id": "abc123",
    "generation": 123,
    "grid": [[...],[...]],
    "meta": null | { "status": "fixed" } | { "status": "cycle", "period": 2 }
  }

-------------------------------------------------------------------------------
License
-------------------------------------------------------------------------------
MIT

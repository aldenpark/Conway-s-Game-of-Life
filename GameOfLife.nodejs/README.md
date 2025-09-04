# Game of Life — Node.js API & CLI

Node.js/Express implementation with SQLite and JSON structured logs. 


## Project layout
```
game-of-life.nodejs/
├─ package.json
├─ .env                  # optional: DB_PATH override
├─ README.md
├─ bin/
│  └─ life-cli.js        # single-file CLI (reuses src/models/life.js)
├─ src/
│  ├─ server.js          # Express app entry (createApp + start)
│  ├─ config.js          # constants (MAX_DIM, MAX_CELLS, DB_PATH)
│  ├─ logger.js          # pino JSON logger + middleware
│  ├─ routes/
│  │  └─ boards.js       # /boards endpoints
│  ├─ db/
│  │  └─ repository.js   # SQLite repo, zlib pack/unpack
│  └─ models/
│     └─ life.js         # LifeConfig + GameOfLife (wrap/nowrap, final_state, etc.)
```

## Install & Run

```bash
# server
npm install
npm run dev      # http://localhost:8000
# or
npm start

# cli
npx life-cli --height 20 --width 50 --density 0.3 --steps 10
npx life-cli --animate --fps 12 --seed 42
```

## API Examples (curl)

Base URL (default): `http://localhost:8000`

Health
```bash
curl -s http://localhost:8000/health
# => {"ok":true}
```

Create a board (random)
```bash
curl -sX POST http://localhost:8000/boards \
  -H 'Content-Type: application/json' \
  -d '{"height":20,"width":40,"density":0.25,"wrap":true,"seed":42}'
# => {"board_id":"<id>"}
```

Create a board (explicit grid)
```bash
curl -sX POST http://localhost:8000/boards \
  -H 'Content-Type: application/json' \
  -d '{"grid":[[0,1,0],[1,1,1],[0,1,0]],"wrap":true}'
# => {"board_id":"<id>"}
```

Get current state
```bash
ID=... # board_id from create
curl -s http://localhost:8000/boards/$ID
# => {"board_id":"...","generation":0,"grid":[[...],[...]]}
```

Advance one generation
```bash
curl -s http://localhost:8000/boards/$ID/next
# => {"board_id":"...","generation":1,"grid":[[...],[...]]}
```

Advance N generations
```bash
curl -s "http://localhost:8000/boards/$ID/advance?n=100"
# => {"board_id":"...","generation":101,"grid":[[...],[...]]}
```

Advance to final state (fixed or cycle)
```bash
curl -s "http://localhost:8000/boards/$ID/final?max_iters=200000"
# => {"board_id":"...","generation":<n>,"grid":[[...],[...]],"meta":{"status":"fixed"}}
# or  {"meta":{"status":"cycle","period":<p>}}
```

Notes
- Limits: `MAX_DIM` caps height/width and `MAX_CELLS` caps total cells.
- Persistence: SQLite file path controlled by `DB_PATH` (default: `life.db`).
- Logs: structured JSON via pino; adjust verbosity with `LOG_LEVEL`.

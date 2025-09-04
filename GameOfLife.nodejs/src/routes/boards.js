// REST endpoints for creating and evolving Game of Life boards.
// This router is intentionally small and delegates simulation and storage to
// dedicated modules.
const express = require('express');
const { v4: uuidv4 } = require('uuid');
const { GameOfLife, LifeConfig } = require('../models/life');
const { Repository } = require('../db/repository');
const { MAX_DIM, MAX_CELLS } = require('../config');

// Ensure `grid` is a rectangular 2D array of 0/1 values.
function validateGridShape(grid) {
  if (!Array.isArray(grid) || grid.length === 0) throw new Error('grid must be a non-empty 2D list');
  const h = grid.length;
  const w = grid[0].length;
  for (const row of grid) {
    if (!Array.isArray(row) || row.length !== w) throw new Error('all rows must be the same length');
    for (const v of row) if (v !== 0 && v !== 1) throw new Error('grid values must be 0 or 1');
  }
  return [h, w];
}

// Guardrails to prevent excessively large boards.
function enforceLimits(height, width) {
  if (height > MAX_DIM || width > MAX_DIM) throw new Error(`height/width must be <= ${MAX_DIM}`);
  if (height * width > MAX_CELLS) throw new Error(`grid cells must be <= ${MAX_CELLS}`);
}

function buildRouter(repo = new Repository()) {
  const router = express.Router();
  router.use(express.json({ limit: '5mb' }));

  router.get('/', (req, res) => {
    res.json({ status: 'ok', message: 'Conway API is running. See /health.' });
  });

  router.get('/health', (req, res) => res.json({ ok: true }));

  // Create a board from an explicit grid or from random parameters.
  router.post('/boards', async (req, res) => {
    try {
      const { grid, height, width, density = 0.25, seed = null, wrap = true } = req.body || {};
      let life;
      if (!grid) {
        if (!(Number.isInteger(height) && Number.isInteger(width) && height > 0 && width > 0)) {
          return res.status(400).json({ detail: 'Provide grid OR height+width for random board.' });
        }
        enforceLimits(height, width);
        life = GameOfLife.fromRandom(height, width, density, seed, new LifeConfig({ wrap }));
      } else {
        const [h, w] = validateGridShape(grid);
        enforceLimits(h, w);
        life = new GameOfLife(grid, new LifeConfig({ wrap }));
      }
      const id = uuidv4().replace(/-/g, '');
      const g = life.toList();
      await repo.saveBoard(id, g, g.length, g[0].length, life.config.wrap, 0);
      res.json({ board_id: id });
    } catch (e) {
      res.status(400).json({ detail: String(e.message || e) });
    }
  });

  // Read the current grid state for a board.
  router.get('/boards/:id', async (req, res) => {
    const b = await repo.getBoard(req.params.id);
    if (!b) return res.status(404).json({ detail: 'Board not found' });
    res.json({ board_id: b.id, generation: b.generation, grid: b.grid });
  });

  // Advance by exactly one generation.
  router.get('/boards/:id/next', async (req, res) => {
    const b = await repo.getBoard(req.params.id);
    if (!b) return res.status(404).json({ detail: 'Board not found' });
    const life = new GameOfLife(b.grid, new LifeConfig({ wrap: b.wrap }));
    life.step();
    const gen = b.generation + 1;
    await repo.saveBoard(b.id, life.toList(), b.height, b.width, b.wrap, gen);
    res.json({ board_id: b.id, generation: gen, grid: life.toList() });
  });

  // Advance by N generations (0..1,000,000).
  router.get('/boards/:id/advance', async (req, res) => {
    const n = parseInt(req.query.n ?? '0', 10);
    if (!(Number.isInteger(n) && n >= 0 && n <= 1000000)) {
      return res.status(400).json({ detail: 'n must be int between 0 and 1_000_000' });
    }
    const b = await repo.getBoard(req.params.id);
    if (!b) return res.status(404).json({ detail: 'Board not found' });
    const life = new GameOfLife(b.grid, new LifeConfig({ wrap: b.wrap }));
    life.stepN(n);
    const gen = b.generation + n;
    await repo.saveBoard(b.id, life.toList(), b.height, b.width, b.wrap, gen);
    res.json({ board_id: b.id, generation: gen, grid: life.toList() });
  });

  // Advance until a fixed point or cycle is reached (bounded by max_iters).
  router.get('/boards/:id/final', async (req, res) => {
    const maxIters = Math.min(Math.max(parseInt(req.query.max_iters ?? '200000', 10), 1), 5000000);
    const b = await repo.getBoard(req.params.id);
    if (!b) return res.status(404).json({ detail: 'Board not found' });
    const life = new GameOfLife(b.grid, new LifeConfig({ wrap: b.wrap }));
    const [finalGrid, info] = life.finalState(maxIters);

    if (info.status === 'maxed') {
      return res.status(422).json({ detail: `No stable/cycle within ${maxIters} iterations` });
    }
    const gen = b.generation + info.iterations;
    await repo.saveBoard(b.id, finalGrid, b.height, b.width, b.wrap, gen);

    const meta = { status: info.status };
    if (info.status === 'cycle') meta.period = Number(info.period || 0);

    res.json({ board_id: b.id, generation: gen, grid: finalGrid, meta });
  });

  return router;
}

module.exports = { buildRouter };

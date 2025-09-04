// Core Game of Life implementation. Provides deterministic generation, optional
// toroidal wrapping, and utilities to advance to a final state (fixed or cycle).
const crypto = require('crypto');

// Configuration toggles for the simulation.
class LifeConfig {
  constructor({ wrap = true } = {}) {
    this.wrap = wrap;
  }
}

class GameOfLife {
  constructor(grid, config = new LifeConfig()) {
    // Expect a 2D array of 0/1 values (ints). Coerce truth to 1 and false to 0.
    if (!Array.isArray(grid) || !Array.isArray(grid[0])) {
      throw new Error('grid must be a 2D array');
    }
    this.grid = grid.map(row => row.map(v => (v ? 1 : 0)));
    this.config = config;
  }

  static fromRandom(height, width, density = 0.25, seed = undefined, config = new LifeConfig()) {
    // Use Math.random by default; if a numeric seed is provided, switch to a
    // tiny LCG for reproducible board generation across runs.
    let rand = Math.random;
    if (Number.isInteger(seed)) {
      let state = BigInt(seed);
      rand = () => {
        state = (1103515245n * state + 12345n) % 2147483648n;
        return Number(state) / 2147483648;
      };
    }
    const g = Array.from({ length: height }, () =>
      Array.from({ length: width }, () => (rand() < density ? 1 : 0))
    );
    return new GameOfLife(g, config);
  }

  toList() {
    return this.grid.map(r => r.slice());
  }

  _neighborsWrap(g) {
    // Compute neighbors on a torus (edges wrap around).
    const h = g.length, w = g[0].length;
    const nbrs = Array.from({ length: h }, () => Array(w).fill(0));
    const idx = (i, n) => (i + n) % n;

    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        let s = 0;
        for (let dy = -1; dy <= 1; dy++) {
          for (let dx = -1; dx <= 1; dx++) {
            if (dx === 0 && dy === 0) continue;
            s += g[idx(y + dy, h)][idx(x + dx, w)];
          }
        }
        nbrs[y][x] = s;
      }
    }
    return nbrs;
  }

  _neighborsNoWrap(g) {
    // Compute neighbors with hard boundaries (no wrap).
    const h = g.length, w = g[0].length;
    const nbrs = Array.from({ length: h }, () => Array(w).fill(0));
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        let s = 0;
        for (let dy = -1; dy <= 1; dy++) {
          for (let dx = -1; dx <= 1; dx++) {
            if (dx === 0 && dy === 0) continue;
            const ny = y + dy, nx = x + dx;
            if (ny >= 0 && ny < h && nx >= 0 && nx < w) s += g[ny][nx];
          }
        }
        nbrs[y][x] = s;
      }
    }
    return nbrs;
  }

  step() {
    // Advance one generation using the classic Conway rules:
    // - A cell survives if it has exactly 2 neighbors.
    // - A cell (dead or alive) becomes alive if it has exactly 3 neighbors.
    const g = this.grid;
    const nbrs = this.config.wrap ? this._neighborsWrap(g) : this._neighborsNoWrap(g);
    const h = g.length, w = g[0].length;
    const next = Array.from({ length: h }, () => Array(w).fill(0));
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        const alive = g[y][x] === 1;
        const n = nbrs[y][x];
        next[y][x] = (n === 3 || (alive && n === 2)) ? 1 : 0;
      }
    }
    this.grid = next;
    return this.grid;
  }

  stepN(n) {
    for (let i = 0; i < Math.max(0, n); i++) this.step();
    return this.grid;
  }

  static _hashGrid(g) {
    // Compactly hash the grid contents to detect repeats efficiently.
    // Using row bytes avoids JSON overhead in the hot path.
    const hasher = crypto.createHash('sha256');
    for (const row of g) hasher.update(Buffer.from(row));
    return hasher.digest('hex');
  }

  finalState(maxIters = 100000) {
    // Iterate until we reach a fixed point or find a cycle. Track prior states
    // by hash to avoid O(N) comparisons each iteration.
    const seen = new Map(); // hash -> first index
    let i = 0;
    while (i <= maxIters) {
      const h = GameOfLife._hashGrid(this.grid);
      if (seen.has(h)) {
        const first = seen.get(h);
        const period = i - first;
        return [this.grid, { iterations: i, status: 'cycle', first_seen_at: first, period }];
      }
      seen.set(h, i);
      const prev = this.toList();
      this.step();
      const same = prev.every((row, y) => row.every((v, x) => v === this.grid[y][x]));
      if (same) {
        return [this.grid, { iterations: i + 1, status: 'fixed' }];
      }
      i++;
    }
    return [this.grid, { iterations: maxIters, status: 'maxed' }];
  }
}

module.exports = { GameOfLife, LifeConfig };

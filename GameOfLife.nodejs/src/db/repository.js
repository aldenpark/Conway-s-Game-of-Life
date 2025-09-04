// Simple SQLite repository for persisting board metadata and state blobs.
// Boards hold dimensions/wrap/generation and state rows store a compressed
// JSON representation of the grid for portability and simplicity.
const sqlite3 = require('sqlite3').verbose();
const zlib = require('zlib');
const { DB_PATH } = require('../config');

class Repository {
  constructor(dbPath = DB_PATH) {
    this.dbPath = dbPath;
    this.db = new sqlite3.Database(dbPath);
    this.db.serialize(() => {
      // WAL mode (Write-Ahead Logging) improves concurrency and crash safety for write-heavy flows 
      // by writing changes to a separate log file before applying them to the main database.
      this.db.run(`
        PRAGMA journal_mode=WAL;
      `);
      this.db.run(`
        CREATE TABLE IF NOT EXISTS boards (
          id TEXT PRIMARY KEY,
          height INTEGER NOT NULL,
          width INTEGER NOT NULL,
          wrap INTEGER NOT NULL,
          generation INTEGER NOT NULL DEFAULT 0
        );
      `);
      this.db.run(`
        CREATE TABLE IF NOT EXISTS states (
          board_id TEXT PRIMARY KEY,
          grid BLOB NOT NULL,
          FOREIGN KEY(board_id) REFERENCES boards(id) ON DELETE CASCADE
        );
      `);
    });
  }

  _pack(grid2D) {
    // Store as gzip-compressed JSON to keep it portable and compact.
    const json = JSON.stringify(grid2D);
    return zlib.gzipSync(Buffer.from(json, 'utf8'));
  }

  _unpack(blob) {
    // Reverse of _pack: gunzip and parse JSON back to a 2D array.
    const json = zlib.gunzipSync(blob).toString('utf8');
    return JSON.parse(json);
  }

  getBoard(id) {
    return new Promise((resolve) => {
      this.db.get(
        `SELECT id,height,width,wrap,generation FROM boards WHERE id=?`,
        [id],
        (err, boardRow) => {
          if (err || !boardRow) return resolve(null);
          this.db.get(`SELECT grid FROM states WHERE board_id=?`, [id], (err2, stateRow) => {
            if (err2 || !stateRow) return resolve(null);
            const grid = this._unpack(stateRow.grid);
            resolve({
              id: boardRow.id,
              height: boardRow.height,
              width: boardRow.width,
              // SQLite has no native boolean; coerce integer flag back to boolean.
              wrap: Boolean(boardRow.wrap),
              generation: boardRow.generation,
              grid
            });
          });
        }
      );
    });
  }

  saveBoard(id, grid, height, width, wrap, generation) {
    return new Promise((resolve, reject) => {
      const blob = this._pack(grid);
      this.db.serialize(() => {
        // Upsert board metadata and associated state atomically in sequence.
        this.db.run(
          `INSERT OR REPLACE INTO boards (id,height,width,wrap,generation) VALUES (?,?,?,?,?)`,
          [id, height, width, wrap ? 1 : 0, generation]
        );
        this.db.run(
          `INSERT OR REPLACE INTO states (board_id,grid) VALUES (?,?)`,
          [id, blob],
          (err) => (err ? reject(err) : resolve())
        );
      });
    });
  }
}

module.exports = { Repository };

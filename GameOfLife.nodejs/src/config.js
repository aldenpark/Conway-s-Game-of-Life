// Load environment variables from `.env` if present.
// This keeps runtime configuration simple and explicit.
require('dotenv').config();

// Path to the SQLite database file. Defaults to a local file in the project.
const DB_PATH = process.env.DB_PATH || 'life.db';

// Hard caps to avoid excessive memory/CPU usage from very large boards.
const MAX_DIM = parseInt(process.env.MAX_DIM || '1000', 10);
const MAX_CELLS = parseInt(process.env.MAX_CELLS || '1000000', 10);

// Export a small, explicit config surface.
module.exports = { DB_PATH, MAX_DIM, MAX_CELLS };

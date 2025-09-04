// Express bootstrapper: wires logging, routes, and error handling.
const express = require('express');
const { httpLogger, logger } = require('./logger');
const { buildRouter } = require('./routes/boards');

function createApp() {
  const app = express();
  app.use(httpLogger); // structured JSON logs
  app.use('/', buildRouter());
  // basic 404
  app.use((req, res) => res.status(404).json({ detail: 'Not found' }));
  // error handler
  // eslint-disable-next-line no-unused-vars
  app.use((err, req, res, next) => {
    logger.error({ err: String(err) }, 'unhandled');
    res.status(500).json({ detail: 'Internal Server Error' });
  });
  return app;
}

// Allow using as a library (tests) or as a runnable script.
if (require.main === module) {
  const app = createApp();
  const port = process.env.PORT || 8000;
  app.listen(port, () => {
    // match your FastAPI root message semantics
    logger.info({ port }, 'Conway API is running');
  });
}

module.exports = { createApp };

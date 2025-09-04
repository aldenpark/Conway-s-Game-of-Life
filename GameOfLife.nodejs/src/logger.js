// Centralized JSON logging via pino. The HTTP variant wires
// request/response metadata into structured logs for easy analysis.
const pino = require('pino');
const pinoHttp = require('pino-http');

// App-level logger. We keep the log envelope minimal and add an epoch timestamp
// so logs are compact but still sortable and machine-friendly.
const logger = pino({
  level: process.env.LOG_LEVEL || 'info',
  base: undefined, // remove pid, hostname for lean logs
  timestamp: () => `,"time":${Date.now()}`
});

// Express middleware which logs each HTTP request with a compact shape.
const httpLogger = pinoHttp({
  logger,
  customSuccessMessage: function (req, res) {
    return 'request';
  },
  customLogLevel: function (req, res, err) {
    if (res.statusCode >= 500 || err) return 'error';
    if (res.statusCode >= 400) return 'warn';
    return 'info';
  },
  serializers: {
    req(req) {
      // Avoid logging full headers/body by default; keep essentials only.
      return {
        method: req.method,
        path: req.url,
        query: req.url.split('?')[1] || '',
        client: req.socket?.remoteAddress
      };
    },
    res(res) {
      return { status: res.statusCode };
    }
  }
});

module.exports = { logger, httpLogger };
